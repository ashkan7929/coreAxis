using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.Workflow.Application.Services;

public interface IWorkflowValidator
{
    Task<Result<bool>> ValidateDslAsync(string dslJson);
}
