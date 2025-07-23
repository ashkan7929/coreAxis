using CoreAxis.SharedKernel.IntegrationEvents;

namespace CoreAxis.Modules.AuthModule.Application.IntegrationEvents;

public class UserRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; }
    public string Username { get; }
    public string Email { get; }
    public Guid TenantId { get; }

    public UserRegisteredIntegrationEvent(Guid userId, string username, string email, Guid tenantId)
    {
        UserId = userId;
        Username = username;
        Email = email;
        TenantId = tenantId;
    }
}