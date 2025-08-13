using CoreAxis.EventBus;

namespace CoreAxis.Modules.AuthModule.Application.IntegrationEvents;

public class UserRegisteredIntegrationEvent : IntegrationEvent, IIntegrationEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    
    public UserRegisteredIntegrationEvent(Guid userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}