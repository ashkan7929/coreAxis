using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductBuilderModule.Domain.Entities;

public class ProductBinding : EntityBase
{
    public Guid ProductVersionId { get; set; }
    public ProductVersion ProductVersion { get; set; } = default!;
    
    public string? WorkflowDefinitionCode { get; set; }
    public string? WorkflowVersionNumber { get; set; }
    
    public Guid? InitialFormId { get; set; }
    public string? InitialFormVersion { get; set; }
    
    public Guid? MappingSetId { get; set; }
    
    public Guid? FormulaId { get; set; }
    public string? FormulaVersion { get; set; }
    
    public Guid? PaymentConfigId { get; set; }
    public Guid? OrderTemplateId { get; set; }
}
