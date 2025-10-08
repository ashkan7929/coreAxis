# CoreAxis

## Overview

CoreAxis is a comprehensive framework for building enterprise web applications using Clean Architecture and SOLID principles. It features a modular design that allows for the development and integration of independent units.

## Project Structure

```
CoreAxis/
├── ApiGateway/                  # Main API Gateway
├── src/
│   ├── ApiGateway/              # API Gateway project
│   ├── BuildingBlocks/          # Shared core components
│   │   └── SharedKernel/        # Shared kernel for the project
│   ├── EventBus/                # Event bus system
│   ├── Infrastructure/          # Shared infrastructure
│   ├── Adapters/                # External service adapters
│   │   └── Stubs/               # Test stubs and mocks
│   └── Modules/                 # Business modules
│       ├── DemoModule/          # Illustrative module
│       ├── ProductOrderModule/  # Product and order management
│       ├── WalletModule/        # Wallet and balance management
│       └── MLMModule/           # Multi-level marketing
│           ├── API/             # Application Programming Interface
│           ├── Application/     # Application layer
│           ├── Domain/          # Domain layer
│           └── Infrastructure/  # Infrastructure layer
├── Tests/                       # Tests
│   └── CoreAxis.Tests/          # Tests project
└── docs/                        # Documentation
```

## Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or newer
- SQL Server (optional, in-memory database can be used for development)

## Quick Start

### Project Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/your-organization/CoreAxis.git
    cd CoreAxis
    ```

2.  Restore packages and build the project:
    ```bash
    dotnet restore
    dotnet build
    ```

3.  Run the project:
    ```bash
    cd src/ApiGateway/CoreAxis.ApiGateway
    dotnet run
    ```

4.  Access the application:
    -   API Interface: `https://localhost:5001/api`
    -   Health Dashboard: `https://localhost:5001/health-ui`

## Key Features

-   **Clean Architecture**: Clear separation between domain, application, infrastructure, and user interface layers
-   **Modular Design**: Independent modules that can be developed and deployed separately
-   **Event-Driven Architecture**: Communication between modules via integration events using MediatR and Outbox pattern
-   **High-Precision Calculations**: Support for decimal precision up to 18,8 for financial operations
-   **Idempotency Support**: Built-in idempotency handling for critical operations
-   **Multi-language Support**: Full support for localization and translation
-   **Health Checks**: Monitoring application and service status
-   **API Documentation**: Automatic documentation using Swagger
-   **Comprehensive Testing**: Unit, integration, and end-to-end tests with high coverage

## Module Development

To add a new module, follow the structure of DemoModule and use the guidelines in ModuleـDevelopmentـGuideline.txt.

Each module should contain:

1.  Domain Layer: Entities and business rules
2.  Application Layer: Application services and use cases
3.  Infrastructure Layer: Interface implementation and data access
4.  API Layer: Controllers and module registration

## Tests

The project includes comprehensive test coverage:

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific module tests
dotnet test --filter "ProductOrderModule"
dotnet test --filter "WalletModule"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Unit Tests**: Domain logic, value objects, and business rules
- **Integration Tests**: Module interactions and event handling
- **Precision Tests**: High-precision decimal calculations and Money value object
- **Idempotency Tests**: Ensuring operations can be safely retried

## Documentation

Refer to the `docs/` folder for detailed documentation on:

-   System Architecture (ARCHITECTURE.md)
-   Health Checks (HealthChecks.md)
-   Continuous Integration (ContinuousIntegration.md)
-   Precision Alignment (precision-alignment.md) - Decimal precision analysis between modules

### API Documentation (Swagger)

- Gateway Swagger UI: `http://localhost:5077/swagger`
- Web API Swagger UI: `http://localhost:5001/swagger` (enable by setting `EnableSwagger=true` in configuration for non-development environments)

### Module-Specific Documentation

- **ProductOrderModule**: Order management, pricing, and workflow integration
- **WalletModule**: Balance management and transaction handling
- **MLMModule**: Multi-level marketing and commission calculations

## Contribution

1.  Follow the code review guidelines in CodeـReview.txt
2.  Ensure tests exist for every new feature
3.  Keep documentation updated

## License

This project is licensed under the [MIT License](LICENSE).