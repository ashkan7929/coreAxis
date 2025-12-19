namespace CoreAxis.Modules.ProductBuilderModule.Application.DTOs;

public class ProductBindingDto
{
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
