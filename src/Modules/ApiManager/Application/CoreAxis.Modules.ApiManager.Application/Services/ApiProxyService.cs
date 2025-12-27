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

public class ApiProxyService : IApiProxy
{
    private static readonly Meter s_meter = new("CoreAxis.ApiManager", "1.0.0");
    private static readonly Counter<long> s_retryCounter = s_meter.CreateCounter<long>("apim_retry_count", unit: "count", description: "Retry attempts in ApiManager");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiProxyService> _logger;
    private readonly DbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthSchemeHandlerResolver _authResolver;
    private readonly ILoggingMaskingService _maskingService;
    private readonly IMemoryCache _cache;

    public ApiProxyService(IHttpClientFactory httpClientFactory, ILogger<ApiProxyService> logger, DbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuthSchemeHandlerResolver authResolver, ILoggingMaskingService maskingService, IMemoryCache cache)
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
    public ApiProxyService(IHttpClientFactory httpClientFactory, ILogger<ApiProxyService> logger, DbContext dbContext)
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
        var httpContext = _httpContextAccessor?.HttpContext;
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

            // Create Call Log
            var callLog = new WebServiceCallLog(method.WebServiceId, method.Id, correlationId, workflowRunId, stepId);
            callLog.LastModifiedBy = "system";
            _dbContext.Set<WebServiceCallLog>().Add(callLog);

            // Build HTTP request
            var httpClient = _httpClientFactory.CreateClient();
            var request = await BuildHttpRequestAsync(method, parameters, correlationId, tenantId, cancellationToken);

            // Log masked request
            var maskingRules = MaskingHelper.ExtractRules(method);
            callLog.SetRequest(await DumpRequestAsync(request, maskingRules));
            
            _logger.LogInformation("Invoking {Method} {Url} with CorrelationId {CorrelationId}", 
                method.HttpMethod, request.RequestUri, correlationId);

            // Execute request with policies
            var policy = CreateResiliencePipeline(method);
            var response = await policy.ExecuteAsync(async () => 
            {
                return await httpClient.SendAsync(request, cancellationToken);
            });

            stopwatch.Stop();

            // Process response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            
            // Log response
            callLog.SetResponse(MaskingHelper.MaskBody(responseBody, _maskingService, maskingRules), (int)response.StatusCode, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiProxyResult.Success((int)response.StatusCode, responseBody, stopwatch.ElapsedMilliseconds, responseHeaders);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invoking method {MethodId}", webServiceMethodId);
            return ApiProxyResult.Failure(ex.Message, stopwatch.ElapsedMilliseconds, 500);
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

    public async Task<ApiProxyResult> InvokeWithExplicitRequestAsync(
        Guid webServiceMethodId,
        Dictionary<string, string> headers,
        Dictionary<string, string> query,
        string? body,
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = _httpContextAccessor?.HttpContext;
        var correlationId = httpContext?.GetCorrelationId() ?? Guid.NewGuid().ToString();
        var tenantId = httpContext?.GetTenantId();

        try
        {
            var method = await _dbContext.Set<WebServiceMethod>()
                .Include(m => m.WebService)
                .ThenInclude(ws => ws.SecurityProfile)
                .FirstOrDefaultAsync(m => m.Id == webServiceMethodId && m.IsActive, cancellationToken);

            if (method == null) return ApiProxyResult.Failure("Method not found or inactive", 0, 404);
            if (!method.WebService.IsActive) return ApiProxyResult.Failure("Service inactive", 0, 400);

            // Create Call Log
            var callLog = new WebServiceCallLog(method.WebServiceId, method.Id, correlationId, workflowRunId, stepId);
            callLog.LastModifiedBy = "system";
            _dbContext.Set<WebServiceCallLog>().Add(callLog);

            // Build Request
            var baseUrl = method.WebService.BaseUrl.TrimEnd('/');
            var path = method.Path;
            var url = $"{baseUrl}{path}";
            if (query != null && query.Any())
            {
                var qs = string.Join("&", query.Select(k => $"{k.Key}={Uri.EscapeDataString(k.Value)}"));
                url += "?" + qs;
            }

            var request = new HttpRequestMessage(new HttpMethod(method.HttpMethod), url);

            if (headers != null)
            {
                foreach (var h in headers) request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            if (!string.IsNullOrEmpty(correlationId)) request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
            if (!string.IsNullOrEmpty(tenantId)) request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

            if (method.WebService.SecurityProfile != null)
            {
                await _authResolver.ApplyAsync(request, method.WebService.SecurityProfile, cancellationToken);
            }

            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            // Log Request
            var maskingRules = MaskingHelper.ExtractRules(method);
            callLog.SetRequest(await DumpRequestAsync(request, maskingRules));
            _logger.LogInformation("Invoking Explicit {Method} {Url}", method.HttpMethod, url);

            // Execute
            var httpClient = _httpClientFactory.CreateClient();
            var policy = CreateResiliencePipeline(method);
            
            var response = await policy.ExecuteAsync(async () => await httpClient.SendAsync(request, cancellationToken));
            
            stopwatch.Stop();
            var respBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var respHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));

            callLog.SetResponse(MaskingHelper.MaskBody(respBody, _maskingService, maskingRules), (int)response.StatusCode, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiProxyResult.Success((int)response.StatusCode, respBody, stopwatch.ElapsedMilliseconds, respHeaders);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invoking explicit method {MethodId}", webServiceMethodId);
            return ApiProxyResult.Failure(ex.Message, stopwatch.ElapsedMilliseconds, 500);
        }
    }

    private async Task<HttpRequestMessage> BuildHttpRequestAsync(WebServiceMethod method, 
        Dictionary<string, object> parameters, string? correlationId, string? tenantId, CancellationToken cancellationToken)
    {
        var baseUrl = method.WebService.BaseUrl.TrimEnd('/');
        var path = method.Path;
        var queryParams = new List<string>();
        var headers = new Dictionary<string, string>();
        object? bodyContent = null;

        foreach (var param in method.Parameters)
        {
            var value = parameters.GetValueOrDefault(param.Name)?.ToString();
            
            if (string.IsNullOrEmpty(value))
            {
                if (param.IsRequired) throw new ArgumentException($"Required parameter '{param.Name}' is missing");
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

        var url = $"{baseUrl}{path}";
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        var request = new HttpRequestMessage(new HttpMethod(method.HttpMethod), url);

        foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (!string.IsNullOrEmpty(correlationId)) request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        if (!string.IsNullOrEmpty(tenantId)) request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

        if (method.WebService.SecurityProfile != null)
        {
            await _authResolver.ApplyAsync(request, method.WebService.SecurityProfile, cancellationToken);
        }

        if (bodyContent != null)
        {
            if (bodyContent is string stringContent)
                request.Content = new StringContent(stringContent, Encoding.UTF8, "application/json");
            else
                request.Content = JsonContent.Create(bodyContent);
        }

        return request;
    }

    private IAsyncPolicy<HttpResponseMessage> CreateResiliencePipeline(WebServiceMethod method)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));
    }

    private async Task<string> DumpRequestAsync(HttpRequestMessage request, MaskingRules maskingRules)
    {
        // Simple dump implementation
        var sb = new StringBuilder();
        sb.AppendLine($"{request.Method} {request.RequestUri}");
        foreach (var header in request.Headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
        }
        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();
            sb.AppendLine();
            sb.AppendLine(MaskingHelper.MaskBody(body, _maskingService, maskingRules));
        }
        return sb.ToString();
    }
}
