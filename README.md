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
│   └── Modules/                 # Various modules
│       └── DemoModule/          # Illustrative module
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
-   **Event System**: Communication between modules via events using MediatR
-   **Multi-language Support**: Full support for localization and translation
-   **Health Checks**: Monitoring application and service status
-   **API Documentation**: Automatic documentation using Swagger

## Module Development

To add a new module, follow the structure of DemoModule and use the guidelines in ModuleـDevelopmentـGuideline.txt.

Each module should contain:

1.  Domain Layer: Entities and business rules
2.  Application Layer: Application services and use cases
3.  Infrastructure Layer: Interface implementation and data access
4.  API Layer: Controllers and module registration

## Tests

Run tests:

```bash
dotnet test
```

## Documentation

Refer to the `docs/` folder for detailed documentation on:

-   System Architecture (ARCHITECTURE.md)
-   Health Checks (HealthChecks.md)
-   Continuous Integration (ContinuousIntegration.md)

## Contribution

1.  Follow the code review guidelines in CodeـReview.txt
2.  Ensure tests exist for every new feature
3.  Keep documentation updated

## License

This project is licensed under the [MIT License](LICENSE).