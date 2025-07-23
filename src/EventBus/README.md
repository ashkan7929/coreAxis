# EventBus

## Purpose

The Event Bus system is a mechanism for communication between different modules in the system. It allows for decoupling between sender and receiver, enhancing flexibility and scalability.

## Key Components

- **IEventBus**: Core interface for publishing and subscribing to events
- **InMemoryEventBus**: In-memory implementation of the event bus (suitable for development)
- **RabbitMQEventBus**: Implementation using RabbitMQ (suitable for production)
- **EventBusHealthCheck**: Health check for the event bus system

## Event Types

- **Domain Events**: Internal events within a single module
- **Integration Events**: Events between different modules

## How to Use

### Publishing an Event

```csharp
public class SampleEventHandler : IIntegrationEventHandler<SampleEvent>
{
    private readonly IEventBus _eventBus;

    public SampleEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken)
    {
        // Process the event
        await _eventBus.PublishAsync(new AnotherEvent { /* ... */ });
    }
}
```

### Subscribing to an Event

```csharp
// In module registration
public void RegisterServices(IServiceCollection services)
{
    services.AddTransient<SampleEventHandler>();
    services.AddTransient<IIntegrationEventHandler<SampleEvent>, SampleEventHandler>();
}

// In module initialization
public void Initialize(IServiceProvider serviceProvider)
{
    var eventBus = serviceProvider.GetRequiredService<IEventBus>();
    eventBus.Subscribe<SampleEvent, SampleEventHandler>();
}
```