using System.Text;
using System.Text.Json;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ApiManager.Application.Services;

public class ApiProxyService : IApiProxy
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiProxyService> _logger;
    private readonly DbContext _dbContext;

    public ApiProxyService(HttpClient httpClient, ILogger<ApiProxyService> logger, DbContext dbContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ApiProxyResult> InvokeAsync(
        Guid webServiceMethodId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        WebServiceCallLog callLog = null!;
        
        try
        {
            // Get web service and method details
            var method = await _dbContext.Set<WebServiceMethod>()
                .Include(m => m.WebService)
                .ThenInclude(ws => ws.SecurityProfile)
                .Include(m => m.Parameters)
                .FirstOrDefaultAsync(m => m.Id == webServiceMethodId, cancellationToken);

            if (method == null)
            {
                var errorMsg = $"WebService method not found: MethodId={webServiceMethodId}";
                _logger.LogError(errorMsg);
                callLog = new WebServiceCallLog(Guid.Empty, webServiceMethodId);
                callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
                await SaveCallLogAsync(callLog, cancellationToken);
                return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 404);
            }

            // Create call log with correct WebServiceId
            callLog = new WebServiceCallLog(method.WebServiceId, webServiceMethodId);

            if (!method.IsActive || !method.WebService.IsActive)
            {
                var errorMsg = "WebService or method is not active";
                _logger.LogWarning(errorMsg);
                callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
                await SaveCallLogAsync(callLog, cancellationToken);
                return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 400);
            }

            // Build request URL
            var baseUrl = method.WebService.BaseUrl.TrimEnd('/');
            var path = method.Path.TrimStart('/');
            var requestUrl = $"{baseUrl}/{path}";

            // Process parameters
            var queryParams = new List<string>();
            var headers = new Dictionary<string, string>();
            string? requestBody = null;

            foreach (var param in method.Parameters.Where(p => p.IsRequired || parameters.ContainsKey(p.Name)))
            {
                if (!parameters.TryGetValue(param.Name, out var value) && param.IsRequired)
                {
                    var errorMsg = $"Required parameter '{param.Name}' is missing";
                    _logger.LogError(errorMsg);
                    callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
                    await SaveCallLogAsync(callLog, cancellationToken);
                    return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 400);
                }

                var paramValue = value?.ToString() ?? param.DefaultValue ?? "";

                switch (param.Location)
                {
                    case ParameterLocation.Query:
                        queryParams.Add($"{param.Name}={Uri.EscapeDataString(paramValue)}");
                        break;
                    case ParameterLocation.Header:
                        headers[param.Name] = paramValue;
                        break;
                    case ParameterLocation.Route:
                        requestUrl = requestUrl.Replace($"{{{param.Name}}}", Uri.EscapeDataString(paramValue));
                        break;
                    case ParameterLocation.Body:
                        requestBody = paramValue;
                        break;
                }
            }

            // Add query parameters to URL
            if (queryParams.Any())
            {
                requestUrl += "?" + string.Join("&", queryParams);
            }

            // Create HTTP request
            using var request = new HttpRequestMessage(new HttpMethod(method.HttpMethod), requestUrl);

            // Add headers
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add security headers if security profile exists
            if (method.WebService.SecurityProfile != null)
            {
                await ApplySecurityProfileAsync(request, method.WebService.SecurityProfile);
            }

            // Add request body for POST/PUT/PATCH
            if (!string.IsNullOrEmpty(requestBody) && 
                (method.HttpMethod == "POST" || method.HttpMethod == "PUT" || method.HttpMethod == "PATCH"))
            {
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            // Set timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(method.TimeoutMs));

            // Log request
            callLog.SetRequest(requestBody);
            _logger.LogInformation("Invoking {Method} {Url}", method.HttpMethod, requestUrl);

            // Execute request
            using var response = await _httpClient.SendAsync(request, cts.Token);
            stopwatch.Stop();

            // Read response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            // Log response
            callLog.SetResponse(responseBody, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode);
            await SaveCallLogAsync(callLog, cancellationToken);

            _logger.LogInformation("Response received: {StatusCode} in {ElapsedMs}ms", 
                response.StatusCode, stopwatch.ElapsedMilliseconds);

            return new ApiProxyResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                ResponseHeaders = responseHeaders
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var errorMsg = "Request was cancelled";
            _logger.LogWarning(errorMsg);
            callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
            await SaveCallLogAsync(callLog, cancellationToken);
            return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 408);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            var errorMsg = "Request timeout";
            _logger.LogError(errorMsg);
            callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
            await SaveCallLogAsync(callLog, cancellationToken);
            return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 408);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            var errorMsg = $"HTTP request failed: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
            await SaveCallLogAsync(callLog, cancellationToken);
            return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 500);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMsg = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            callLog.SetError(errorMsg, stopwatch.ElapsedMilliseconds);
            await SaveCallLogAsync(callLog, cancellationToken);
            return ApiProxyResult.Failure(errorMsg, stopwatch.ElapsedMilliseconds, 500);
        }
    }

    private async Task ApplySecurityProfileAsync(HttpRequestMessage request, SecurityProfile securityProfile)
    {
        try
        {
            if (string.IsNullOrEmpty(securityProfile.ConfigJson))
                return;

            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(securityProfile.ConfigJson);
            if (config == null) return;

            switch (securityProfile.Type)
            {
                case SecurityType.ApiKey:
                    if (config.TryGetValue("headerName", out var headerName) && 
                        config.TryGetValue("apiKey", out var apiKey))
                    {
                        request.Headers.TryAddWithoutValidation(headerName.ToString()!, apiKey.ToString());
                    }
                    break;

                case SecurityType.OAuth2:
                    if (config.TryGetValue("token", out var token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.ToString());
                    }
                    break;


            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply security profile {SecurityProfileId}", securityProfile.Id);
        }
    }

    private async Task SaveCallLogAsync(WebServiceCallLog callLog, CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.Set<WebServiceCallLog>().Add(callLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save call log");
        }
    }
}