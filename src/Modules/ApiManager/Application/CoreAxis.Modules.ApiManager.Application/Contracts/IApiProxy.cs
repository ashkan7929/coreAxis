namespace CoreAxis.Modules.ApiManager.Application.Contracts;

public interface IApiProxy
{
    Task<ApiProxyResult> InvokeAsync(
        Guid webServiceMethodId, 
        Dictionary<string, object> parameters, 
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default);

    Task<ApiProxyResult> InvokeAsync(
        string serviceName,
        string methodName,
        Dictionary<string, object> parameters,
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default);

    Task<ApiProxyResult> InvokeWithExplicitRequestAsync(
        Guid webServiceMethodId,
        Dictionary<string, string> headers,
        Dictionary<string, string> query,
        string? body,
        Guid? workflowRunId = null,
        string? stepId = null,
        CancellationToken cancellationToken = default);
}

public class ApiProxyResult
{
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public long LatencyMs { get; set; }
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();

    public static ApiProxyResult Success(int statusCode, string responseBody, long latencyMs, Dictionary<string, string>? headers = null)
    {
        return new ApiProxyResult
        {
            IsSuccess = true,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            LatencyMs = latencyMs,
            ResponseHeaders = headers ?? new Dictionary<string, string>()
        };
    }

    public static ApiProxyResult Failure(string errorMessage, long latencyMs, int? statusCode = null)
    {
        return new ApiProxyResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            LatencyMs = latencyMs
        };
    }
}