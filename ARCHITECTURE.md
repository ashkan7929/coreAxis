# CoreAxis Architecture

## Overview

CoreAxis is a clean, scalable, modular SaaS platform built with .NET Core, following SOLID principles and clean architecture. It is designed to support with extensible modules and multi-language support. The foundation is future-proof, allowing hundreds of modules to communicate seamlessly.

## Solution Structure

The solution is organized into the following main components:

```
CoreAxis/
├── src/
│   ├── ApiGateway/                 # Main entry point for the application
│   ├── BuildingBlocks/
│   │   ├── SharedKernel/           # Core domain primitives and utilities
│   │   └── BuildingBlocks/         # Common abstractions and interfaces
│   ├── EventBus/                   # Event bus for cross-module communication
│   ├── Infrastructure/             # Shared infrastructure services
│   └── Modules/                    # Individual business modules
│       └── DemoModule/
│           ├── Domain/             # Domain entities, value objects, and interfaces
│           ├── Application/        # Application services and use cases
│           ├── Infrastructure/     # Infrastructure implementations
│           └── API/                # API controllers and DTOs
└── Tests/                          # Test projects
```

## Design Principles

### Clean Architecture

The solution follows the principles of Clean Architecture, with a clear separation of concerns:

1. **Domain Layer**: Contains business entities, value objects, domain events, and repository interfaces. It has no dependencies on other layers.
2. **Application Layer**: Contains application services, use cases, and domain event handlers. It depends only on the Domain layer.
3. **Infrastructure Layer**: Contains implementations of repository interfaces, external services, and data access. It depends on the Domain and Application layers.
4. **API Layer**: Contains controllers, DTOs, and API-specific logic. It depends on the Application layer.

### SOLID Principles

- **Single Responsibility Principle**: Each class has a single responsibility.
- **Open/Closed Principle**: Classes are open for extension but closed for modification.
- **Liskov Substitution Principle**: Derived classes can be substituted for their base classes.
- **Interface Segregation Principle**: Clients should not be forced to depend on interfaces they do not use.
- **Dependency Inversion Principle**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

### Modular Architecture

The solution is designed to be modular, with each module being a self-contained business domain. Modules communicate with each other through events, avoiding tight coupling. Each module follows the same internal structure (Domain, Application, Infrastructure, API).

## Module Lifecycle

### Module Registration

Modules are registered at application startup through the `ModuleRegistrar` class, which scans assemblies for classes implementing the `IModule` interface. Each module can register its own services and configure its own middleware.

```csharp
// In Program.cs
var moduleRegistrar = new ModuleRegistrar();
moduleRegistrar.RegisterModules(builder.Services, builder.Configuration);

// Later in the pipeline
moduleRegistrar.ConfigureModules(app);
```

### Module Communication

Modules communicate with each other through events, using the event bus. There are two types of events:

1. **Domain Events**: Used for communication within a module. Handled synchronously.
2. **Integration Events**: Used for communication between modules. Handled asynchronously.

```csharp
// Publishing an event
await _eventBus.PublishAsync(new DemoItemCreatedEvent(demoItem.Id, demoItem.Name));

// Subscribing to an event
_eventBus.Subscribe<DemoItemCreatedEvent, DemoItemCreatedEventHandler>();
```

## Shared Components

### SharedKernel

The `CoreAxis.SharedKernel` project contains core domain primitives and utilities that are used across all modules:

- **EntityBase**: Base class for all entities, with audit fields and domain event handling.
- **ValueObject**: Base class for value objects, with structural equality comparison.
- **DomainEvent**: Base class for domain events.
- **IntegrationEvent**: Base class for integration events.
- **Result<T>**: A wrapper for operation results, with success/failure status and error messages.
- **PaginatedList<T>**: A generic class for paginated lists.
- **Exceptions**: Common exception types.
- **Localization**: Services for handling localization.

### BuildingBlocks

The `CoreAxis.BuildingBlocks` project contains common abstractions and interfaces that are used across all modules:

- **IModule**: Interface for module registration and configuration.
- **ModuleRegistrar**: Class for discovering and registering modules.

### EventBus

The `CoreAxis.EventBus` project contains the event bus implementation for cross-module communication:

- **IEventBus**: Interface for the event bus.
- **InMemoryEventBus**: In-memory implementation of the event bus.
- **IIntegrationEventHandler**: Interface for integration event handlers.

## Best Practices

### Dependency Injection

All dependencies should be injected through constructor injection. Services should be registered with the appropriate lifetime (Singleton, Scoped, or Transient).

### Async/Await

All I/O operations should be asynchronous, using the `async`/`await` pattern. Methods that perform I/O should return `Task` or `Task<T>`.

### Exception Handling

Exceptions should be handled at the appropriate level. Domain exceptions should be caught and translated to appropriate responses at the API level.

### Validation

Validation should be performed at the domain level, using domain rules. API-level validation should be used for input validation only.

### Localization

All user-facing strings should be localized using the `ILocalizationService`. Each module can include its own resource files.

## Extending the Platform

To create a new module:

1. Create a new folder under `src/Modules` with the module name.
2. Create the four projects: Domain, Application, Infrastructure, and API.
3. Implement the `IModule` interface in the API project.
4. Register domain entities, application services, and infrastructure implementations.
5. Add controllers and DTOs in the API project.
6. Add integration events for cross-module communication.

## Conclusion

The CoreAxis architecture is designed to be clean, scalable, and modular. By following the principles and patterns described in this document, developers can create new modules that integrate seamlessly with the platform.