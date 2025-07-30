using CoreAxis.SharedKernel.DomainEvents;

namespace CoreAxis.Modules.AuthModule.Domain.Events;

public class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Username { get; }
    public string Email { get; }

    public UserRegisteredEvent(Guid userId, string username, string email)
    {
        UserId = userId;
        Username = username;
        Email = email;
    }
}