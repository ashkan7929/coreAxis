using CoreAxis.EventBus;

namespace CoreAxis.SharedKernel.Contracts.Events;

public class UserRegistered : IntegrationEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string TenantId { get; }
    public string SchemaVersion { get; } = "v1";

    public UserRegistered(Guid userId, string email, string tenantId, Guid correlationId, Guid? causationId = null)
        : base(Guid.NewGuid(), DateTime.UtcNow, correlationId, causationId)
    {
        UserId = userId;
        Email = email;
        TenantId = tenantId;
    }
}