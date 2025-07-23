# ApiGateway

## Purpose

The API Gateway is the main entry point for the application. It routes requests to the appropriate modules and provides shared services such as authentication, authorization, health checks, and logging.

## Key Components

- **HealthChecks**: System and service health checks
- **ModuleRegistrar**: Automatic discovery and registration of modules
- **Serilog Configuration**: Central logging setup
- **Middleware**: Middleware for authentication, authorization, and error handling

## How to Use

1. Run the API Gateway:
   ```bash
   cd src/ApiGateway/CoreAxis.ApiGateway
   dotnet run
   ```

2. Access the health dashboard:
   ```
   https://localhost:5001/health-ui
   ```

3. Access the API documentation:
   ```
   https://localhost:5001/swagger
   ```