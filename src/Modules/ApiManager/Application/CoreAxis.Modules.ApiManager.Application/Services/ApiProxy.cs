using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CoreAxis.Modules.ApiManager.Application.Services;

public class ApiProxy : IApiProxy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiProxy> _logger;
    private readonly DbContext _dbContext;

    public ApiProxy(IHttpClientFactory httpClientFactory, ILogger<ApiProxy> logger, DbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ApiProxyResult> InvokeAsync(Guid webServiceMethodId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Load method with related data
            var method = await _dbContext.Set<WebServiceMethod>()
                .Include(m => m.WebService)
                .ThenInclude(ws => ws.SecurityProfile)
                .Include(m => m.Parameters)
                .FirstOrDefaultAsync(m => m.Id == webServiceMethodId && m.IsActive, cancellationToken);

            if (method == null)
            {
                return ApiProxyResult.Failure("WebService method not found or inactive", stopwatch.ElapsedMilliseconds);
            }

            if (!method.WebService.IsActive)
            {
                return ApiProxyResult.Failure("WebService is inactive", stopwatch.ElapsedMilliseconds);
            }

            // Create call log
            var callLog = new WebServiceCallLog(method.WebServiceId, method.Id, correlationId);
            callLog.CreatedBy = "system"; // TODO: Get from current user context
            callLog.LastModifiedBy = "system";
            _dbContext.Set<WebServiceCallLog>().Add(callLog);

            // Build HTTP request
            var httpClient = _httpClientFactory.CreateClient();
            var request = await BuildHttpRequestAsync(method, parameters, cancellationToken);
            
            // Log request
            var requestDump = await DumpRequestAsync(request);
            callLog.SetRequest(requestDump);
            
            _logger.LogInformation("Invoking {Method} {Url} with CorrelationId {CorrelationId}", 
                method.HttpMethod, request.RequestUri, correlationId);

            // Create resilience pipeline for this method
            var policy = CreateResiliencePipeline(method);

            // Execute request with policies
            var response = await policy.ExecuteAsync(async () => 
            {
                return await httpClient.SendAsync(request, cancellationToken);
            });

            stopwatch.Stop();

            // Process response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            
            // Log response
            callLog.SetResponse(responseBody, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, 
                response.IsSuccessStatusCode);
            
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed {Method} {Url} with status {StatusCode} in {LatencyMs}ms", 
                method.HttpMethod, request.RequestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            return ApiProxyResult.Success((int)response.StatusCode, responseBody, 
                stopwatch.ElapsedMilliseconds, responseHeaders);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error invoking method {MethodId} with CorrelationId {CorrelationId}", 
                webServiceMethodId, correlationId);

            // Try to log error if we have a call log
            try
            {
                var callLog = await _dbContext.Set<WebServiceCallLog>()
                    .FirstOrDefaultAsync(cl => cl.CorrelationId == correlationId, cancellationToken);
                
                if (callLog != null)
                {
                    callLog.SetError(ex.Message, stopwatch.ElapsedMilliseconds);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log error for CorrelationId {CorrelationId}", correlationId);
            }

            return ApiProxyResult.Failure(ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePipeline(WebServiceMethod method)
    {
        // Retry policy
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for method {MethodId} in {Delay}ms",
                        retryCount, method.Id, timespan.TotalMilliseconds);
                });
        
        if (!string.IsNullOrEmpty(method.RetryPolicyJson))
        {
            try
            {
                var customRetryConfig = JsonSerializer.Deserialize<RetryConfig>(method.RetryPolicyJson);
                if (customRetryConfig != null)
                {
                    retryPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(
                            retryCount: customRetryConfig.MaxRetryAttempts,
                            sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(customRetryConfig.DelayMs * retryAttempt),
                            onRetry: (outcome, timespan, retryCount, context) =>
                            {
                                _logger.LogWarning("Retry {RetryCount} for method {MethodId} in {Delay}ms",
                                    retryCount, method.Id, timespan.TotalMilliseconds);
                            });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse retry policy JSON for method {MethodId}", method.Id);
            }
        }

        // Circuit breaker policy
        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
        
        if (!string.IsNullOrEmpty(method.CircuitPolicyJson))
        {
            try
            {
                var customCircuitConfig = JsonSerializer.Deserialize<CircuitBreakerConfig>(method.CircuitPolicyJson);
                if (customCircuitConfig != null)
                {
                    circuitBreakerPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .CircuitBreakerAsync(
                            handledEventsAllowedBeforeBreaking: customCircuitConfig.HandledEventsAllowedBeforeBreaking,
                            durationOfBreak: TimeSpan.FromSeconds(customCircuitConfig.BreakDurationSeconds));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse circuit breaker policy JSON for method {MethodId}", method.Id);
            }
        }

        // Timeout policy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(method.TimeoutMs));

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }

    private async Task<HttpRequestMessage> BuildHttpRequestAsync(WebServiceMethod method, 
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var baseUrl = method.WebService.BaseUrl.TrimEnd('/');
        var path = method.Path;
        var queryParams = new List<string>();
        var headers = new Dictionary<string, string>();
        object? bodyContent = null;

        // Process parameters
        foreach (var param in method.Parameters)
        {
            var value = parameters.GetValueOrDefault(param.Name)?.ToString();
            
            if (string.IsNullOrEmpty(value))
            {
                if (param.IsRequired)
                {
                    throw new ArgumentException($"Required parameter '{param.Name}' is missing");
                }
                value = param.DefaultValue;
            }

            if (string.IsNullOrEmpty(value)) continue;

            switch (param.Location)
            {
                case ParameterLocation.Query:
                    queryParams.Add($"{param.Name}={Uri.EscapeDataString(value)}");
                    break;
                case ParameterLocation.Header:
                    headers[param.Name] = value;
                    break;
                case ParameterLocation.Route:
                    path = path.Replace($"{{{param.Name}}}", Uri.EscapeDataString(value));
                    break;
                case ParameterLocation.Body:
                    bodyContent = parameters.GetValueOrDefault(param.Name);
                    break;
            }
        }

        // Build URL
        var url = $"{baseUrl}{path}";
        if (queryParams.Any())
        {
            url += "?" + string.Join("&", queryParams);
        }

        // Create request
        var request = new HttpRequestMessage(new HttpMethod(method.HttpMethod), url);

        // Add headers
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add security headers
        if (method.WebService.SecurityProfile != null)
        {
            await ApplySecurityProfileAsync(request, method.WebService.SecurityProfile, cancellationToken);
        }

        // Add body content
        if (bodyContent != null)
        {
            if (bodyContent is string stringContent)
            {
                request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            }
            else
            {
                request.Content = JsonContent.Create(bodyContent);
            }
        }

        return request;
    }

    private async Task ApplySecurityProfileAsync(HttpRequestMessage request, SecurityProfile securityProfile, 
        CancellationToken cancellationToken)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(securityProfile.ConfigJson);
            if (config == null) return;

            switch (securityProfile.Type)
            {
                case SecurityType.ApiKey:
                    if (config.TryGetValue("headerName", out var headerName) && 
                        config.TryGetValue("apiKey", out var apiKey))
                    {
                        request.Headers.TryAddWithoutValidation(headerName, apiKey);
                    }
                    break;
                case SecurityType.OAuth2:
                    if (config.TryGetValue("token", out var token))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply security profile {ProfileId}", securityProfile.Id);
        }
    }

    private async Task<string> DumpRequestAsync(HttpRequestMessage request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{request.Method} {request.RequestUri}");
        
        foreach (var header in request.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            sb.AppendLine();
            var content = await request.Content.ReadAsStringAsync();
            sb.AppendLine(content);
        }
        
        return sb.ToString();
    }
}

public class RetryConfig
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int DelayMs { get; set; } = 1000;
}

public class CircuitBreakerConfig
{
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 5;
    public int BreakDurationSeconds { get; set; } = 30;
}