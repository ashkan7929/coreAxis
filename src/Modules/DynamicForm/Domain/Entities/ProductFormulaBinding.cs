using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.DynamicForm.Domain.Entities;

/// <summary>
/// Binds a product to a specific formula definition version.
/// </summary>
public class ProductFormulaBinding : EntityBase
{
    /// <summary>
    /// Gets or sets the product identifier.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Gets or sets the formula definition identifier.
    /// </summary>
    public Guid FormulaDefinitionId { get; private set; }

    /// <summary>
    /// Gets or sets the version number of the formula definition.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Gets or sets the tenant identifier for multi-tenancy support.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Navigation property to the formula definition.
    /// </summary>
    public virtual FormulaDefinition? FormulaDefinition { get; private set; }

    private ProductFormulaBinding() { }

    public ProductFormulaBinding(Guid productId, Guid formulaDefinitionId, int versionNumber, string tenantId)
    {
        if (productId == Guid.Empty) throw new ArgumentException("ProductId cannot be empty", nameof(productId));
        if (formulaDefinitionId == Guid.Empty) throw new ArgumentException("FormulaDefinitionId cannot be empty", nameof(formulaDefinitionId));
        if (versionNumber <= 0) throw new ArgumentOutOfRangeException(nameof(versionNumber), "VersionNumber must be greater than zero");
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

        ProductId = productId;
        FormulaDefinitionId = formulaDefinitionId;
        VersionNumber = versionNumber;
        TenantId = tenantId;
    }
}