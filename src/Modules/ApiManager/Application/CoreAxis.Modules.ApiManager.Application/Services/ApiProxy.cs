using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Application.Masking;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.SharedKernel.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CoreAxis.Modules.ApiManager.Application.Services;

public class ApiProxy : IApiProxy
{
    private static readonly Meter s_meter = new("CoreAxis.ApiManager", "1.0.0");
    private static readonly Counter<long> s_retryCounter = s_meter.CreateCounter<long>("apim_retry_count", unit: "count", description: "Retry attempts in ApiManager");
    private static readonly Counter<long> s_circuitOpenCounter = s_meter.CreateCounter<long>("apim_circuit_open", unit: "count", description: "Circuit opened events");
    private static readonly Counter<long> s_circuitResetCounter = s_meter.CreateCounter<long>("apim_circuit_reset", unit: "count", description: "Circuit reset events");
    private static readonly Counter<long> s_circuitHalfOpenCounter = s_meter.CreateCounter<long>("apim_circuit_half_open", unit: "count", description: "Circuit half-open transitions");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiProxy> _logger;
    private readonly DbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthSchemeHandlerResolver _authResolver;
    private readonly ILoggingMaskingService _maskingService;
    private readonly IMemoryCache _cache;

    public ApiProxy(IHttpClientFactory httpClientFactory, ILogger<ApiProxy> logger, DbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuthSchemeHandlerResolver authResolver, ILoggingMaskingService maskingService, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _authResolver = authResolver;
        _maskingService = maskingService;
        _cache = cache;
    }

    // Backward-compatible constructor
    public ApiProxy(IHttpClientFactory httpClientFactory, ILogger<ApiProxy> logger, DbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dbContext = dbContext;
        _httpContextAccessor = null!;
        _authResolver = new NoAuthResolver();
        _maskingService = new NoOpMaskingService();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    private sealed class NoAuthResolver : IAuthSchemeHandlerResolver
    {
        public Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOpMaskingService : ILoggingMaskingService
    {
        public string MaskSensitiveData(string input) => input;
        public string MaskConfigJson(string configJson) => configJson;
        public T MaskSensitiveProperties<T>(T obj) where T : class => obj;
    }

    public async Task<ApiProxyResult> InvokeAsync(
        Guid webServiceMethodId, 
        Dictionary<string, object> parameters, 
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = httpContext?.GetCorrelationId() ?? Guid.NewGuid().ToString();
        var tenantId = httpContext?.GetTenantId();
        
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
            var callLog = new WebServiceCallLog(method.WebServiceId, method.Id, correlationId, workflowRunId, stepId);
            callLog.CreatedBy = "system"; // TODO: Get from current user context
            callLog.LastModifiedBy = "system";
            _dbContext.Set<WebServiceCallLog>().Add(callLog);

            // Build HTTP request
            var httpClient = _httpClientFactory.CreateClient();
            var request = await BuildHttpRequestAsync(method, parameters, correlationId, tenantId, cancellationToken);

            // Determine masking rules
            var maskingRules = MaskingHelper.ExtractRules(method);

            // Log masked request
            var requestDump = await DumpRequestAsync(request, maskingRules);
            callLog.SetRequest(requestDump);
            
            _logger.LogInformation("Invoking {Method} {Url} with CorrelationId {CorrelationId}", 
                method.HttpMethod, request.RequestUri, correlationId);

            // Short-TTL response cache for idempotent methods (APM-7)
            var isIdempotent = string.Equals(method.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(method.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase);
            // Prefer TTL from EndpointConfigJson; fall back to RetryPolicyJson for backward compatibility
            var cacheTtlSeconds = ExtractCacheTtlSeconds(method?.EndpointConfigJson ?? method?.RetryPolicyJson);
            string? bodyForKey = null;
            if (request.Content != null)
            {
                bodyForKey = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            var cacheKey = isIdempotent && cacheTtlSeconds > 0
                ? BuildCacheKey(method.Id, request.RequestUri?.ToString() ?? string.Empty, bodyForKey)
                : null;

            if (!string.IsNullOrEmpty(cacheKey) && _cache.TryGetValue<ApiProxyResult>(cacheKey, out var cachedResult))
            {
                // Log cached response and return
                var maskedCachedBody = MaskingHelper.MaskBody(cachedResult.ResponseBody ?? string.Empty, _maskingService, maskingRules);
                callLog.SetResponse(maskedCachedBody, cachedResult.StatusCode ?? 200, 0, cachedResult.IsSuccess);
                await _dbContext.SaveChangesAsync(cancellationToken);

                cachedResult.ResponseHeaders ??= new Dictionary<string, string>();
                cachedResult.ResponseHeaders["X-Cache-Hit"] = "true";

                _logger.LogInformation("Cache hit for {MethodId} {Url}", method.Id, request.RequestUri);
                return cachedResult;
            }

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
            var maskedResponseBody = MaskingHelper.MaskBody(responseBody, _maskingService, maskingRules);
            callLog.SetResponse(maskedResponseBody, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, 
                response.IsSuccessStatusCode);
            
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed {Method} {Url} with status {StatusCode} in {LatencyMs}ms", 
                method.HttpMethod, request.RequestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            var result = ApiProxyResult.Success((int)response.StatusCode, responseBody, 
                stopwatch.ElapsedMilliseconds, responseHeaders);

            // Populate cache on success
            if (!string.IsNullOrEmpty(cacheKey) && cacheTtlSeconds > 0 && response.IsSuccessStatusCode)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(cacheTtlSeconds));
                _logger.LogInformation("Cached response for {MethodId} {Url} for {Ttl}s", method.Id, request.RequestUri, cacheTtlSeconds);
            }

            return result;
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

    public async Task<ApiProxyResult> InvokeAsync(
        string serviceName,
        string methodName,
        Dictionary<string, object> parameters,
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.WebService)
            .Where(m => m.WebService.Name == serviceName && m.Path == methodName)
            .FirstOrDefaultAsync(cancellationToken);

        if (method == null)
        {
             method = await _dbContext.Set<WebServiceMethod>()
                .Include(m => m.WebService)
                .Where(m => m.WebService.Name == serviceName && m.Path == "/" + methodName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (method == null)
        {
            return ApiProxyResult.Failure($"Method '{methodName}' not found in service '{serviceName}'", 0, 404);
        }

        return await InvokeAsync(method.Id, parameters, workflowRunId, stepId, cancellationToken);
    }

    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePipeline(WebServiceMethod method)
    {
        // Retry policy with exponential backoff + jitter
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                {
                    var baseDelayMs = Math.Pow(2, retryAttempt) * 1000; // exponential seconds to ms
                    var jitterMs = Random.Shared.Next(0, 250);
                    return TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for method {MethodId} in {Delay}ms",
                        retryCount, method.Id, timespan.TotalMilliseconds);
                    s_retryCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
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
                            sleepDurationProvider: retryAttempt =>
                            {
                                // exponential backoff based on configured delay, plus jitter
                                var baseDelayMs = Math.Pow(2, retryAttempt) * customRetryConfig.DelayMs;
                                var jitterMs = Random.Shared.Next(0, 250);
                                return TimeSpan.FromMilliseconds(baseDelayMs + jitterMs);
                            },
                            onRetry: (outcome, timespan, retryCount, context) =>
                            {
                                _logger.LogWarning("Retry {RetryCount} for method {MethodId} in {Delay}ms",
                                    retryCount, method.Id, timespan.TotalMilliseconds);
                                s_retryCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
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
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    _logger.LogWarning("Circuit open for method {MethodId} for {Delay}ms", method.Id, breakDelay.TotalMilliseconds);
                    s_circuitOpenCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit reset for method {MethodId}", method.Id);
                    s_circuitResetCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit half-open for method {MethodId}", method.Id);
                    s_circuitHalfOpenCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                });
        
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
                            durationOfBreak: TimeSpan.FromSeconds(customCircuitConfig.BreakDurationSeconds),
                            onBreak: (outcome, breakDelay) =>
                            {
                                _logger.LogWarning("Circuit open for method {MethodId} for {Delay}ms", method.Id, breakDelay.TotalMilliseconds);
                                s_circuitOpenCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                            },
                            onReset: () =>
                            {
                                _logger.LogInformation("Circuit reset for method {MethodId}", method.Id);
                                s_circuitResetCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                            },
                            onHalfOpen: () =>
                            {
                                _logger.LogInformation("Circuit half-open for method {MethodId}", method.Id);
                                s_circuitHalfOpenCounter.Add(1, new KeyValuePair<string, object?>("method_id", method.Id));
                            });
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
        Dictionary<string, object> parameters, string? correlationId, string? tenantId, CancellationToken cancellationToken)
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

        // Propagate correlation and tenant headers
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }
        if (!string.IsNullOrEmpty(tenantId))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        }

        // Apply security via pluggable handlers
        if (method.WebService.SecurityProfile != null)
        {
            await _authResolver.ApplyAsync(request, method.WebService.SecurityProfile, cancellationToken);
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

    // Security is handled by pluggable handlers via _authResolver

    private async Task<string> DumpRequestAsync(HttpRequestMessage request, MaskingRules? rules)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{request.Method} {request.RequestUri}");
        
        foreach (var header in MaskingHelper.MaskHeaders(request.Headers, _maskingService, rules))
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }
        
        if (request.Content != null)
        {
            foreach (var header in MaskingHelper.MaskHeaders(request.Content.Headers, _maskingService, rules))
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }
            sb.AppendLine();
            var content = await request.Content.ReadAsStringAsync();
            var maskedContent = MaskingHelper.MaskBody(content, _maskingService, rules);
            sb.AppendLine(maskedContent);
        }
        
        return sb.ToString();
    }

    private static int ExtractCacheTtlSeconds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            // Support nested endpoint config: { "endpoint": { "cacheTtlSeconds": 30 } } or flat { "cacheTtlSeconds": 30 }
            if (doc.RootElement.TryGetProperty("cacheTtlSeconds", out var ttlProp) && ttlProp.ValueKind == JsonValueKind.Number)
            {
                var ttl = ttlProp.GetInt32();
                return ttl > 0 ? ttl : 0;
            }
            if (doc.RootElement.TryGetProperty("endpoint", out var endpointProp))
            {
                if (endpointProp.ValueKind == JsonValueKind.Object && endpointProp.TryGetProperty("cacheTtlSeconds", out var nestedTtl) && nestedTtl.ValueKind == JsonValueKind.Number)
                {
                    var ttl = nestedTtl.GetInt32();
                    return ttl > 0 ? ttl : 0;
                }
            }
        }
        catch
        {
            // ignore malformed json
        }
        return 0;
    }

    private static string BuildCacheKey(Guid methodId, string url, string? body)
    {
        var normalizedUrl = url.Trim();
        var bodyHash = string.Empty;
        if (!string.IsNullOrEmpty(body))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(body);
            var hash = sha.ComputeHash(bytes);
            bodyHash = Convert.ToHexString(hash);
        }
        return $"apim:resp:{methodId}:{normalizedUrl}:{bodyHash}";
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