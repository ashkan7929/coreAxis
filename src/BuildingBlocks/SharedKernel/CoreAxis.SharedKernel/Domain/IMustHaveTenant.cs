namespace CoreAxis.SharedKernel.Domain;

public interface IMustHaveTenant
{
    string TenantId { get; set; }
}
