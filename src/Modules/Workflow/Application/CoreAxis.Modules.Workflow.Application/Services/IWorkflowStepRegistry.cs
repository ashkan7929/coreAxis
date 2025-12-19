using CoreAxis.Modules.Workflow.Application.DTOs;

namespace CoreAxis.Modules.Workflow.Application.Services;

public interface IWorkflowStepRegistry
{
    IReadOnlyList<WorkflowStepTypeDescriptor> GetAllStepTypes();
    WorkflowStepTypeDescriptor? GetStepType(string type);
}
