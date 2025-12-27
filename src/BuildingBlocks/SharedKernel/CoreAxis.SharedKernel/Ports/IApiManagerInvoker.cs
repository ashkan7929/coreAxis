using CoreAxisExecutionContext = CoreAxis.SharedKernel.Context.ExecutionContext;

namespace CoreAxis.SharedKernel.Ports;

public interface IApiManagerInvoker
{
    Task<ApiInvokeResult> InvokeAsync(
        string apiMethodRef,
        CoreAxisExecutionContext context,
        string inputMappingSetId,
        string outputMappingSetId,
        bool saveStepIO,
        string stepId,
        CancellationToken ct);
}

public sealed class ApiInvokeResult
{
    public CoreAxisExecutionContext UpdatedContext { get; init; } = default!;
    public int HttpStatusCode { get; init; }
}
