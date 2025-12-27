using CoreAxis.SharedKernel;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Quotes;

public class QuoteWorkflowBinding : EntityBase
{
    public string AssetCode { get; private set; } = string.Empty;
    public string WorkflowCode { get; private set; } = string.Empty;
    public int WorkflowVersion { get; private set; }
    public Guid ReturnMappingSetId { get; private set; }

    private QuoteWorkflowBinding() { } // For EF Core

    public static QuoteWorkflowBinding Create(
        string assetCode,
        string workflowCode,
        int workflowVersion,
        Guid returnMappingSetId)
    {
        return new QuoteWorkflowBinding
        {
            AssetCode = assetCode,
            WorkflowCode = workflowCode,
            WorkflowVersion = workflowVersion,
            ReturnMappingSetId = returnMappingSetId,
            IsActive = true
        };
    }
}
