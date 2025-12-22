namespace CoreAxis.SharedKernel.Context;

public interface ITenantProvider
{
    string? TenantId { get; }
}
