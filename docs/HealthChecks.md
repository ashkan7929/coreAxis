# Health Checks and Observability in CoreAxis

This document describes the health checks and observability features implemented in the CoreAxis platform.

## Health Checks

Health checks provide a way to monitor the health of the application and its dependencies. CoreAxis implements health checks using the ASP.NET Core Health Checks middleware.

### Implemented Health Checks

1. **Self Check**: Verifies that the API Gateway itself is running.
2. **Event Bus Check**: Verifies that the event bus is operational.

### Health Check Endpoints

The following health check endpoints are available:

- `/health`: Returns the health of all registered health checks.
- `/health/live`: Returns the health of the API Gateway itself (liveness probe).
- `/health/ready`: Returns the health of all dependencies (readiness probe).

### Health Check Dashboard

A simple health check dashboard is available at `/health-dashboard/index.html`. This dashboard provides a visual representation of the health of the application and its dependencies.

### Adding New Health Checks

To add a new health check:

1. Create a new class that implements `IHealthCheck`.
2. Register the health check in the `AddCoreAxisHealthChecks` method in `HealthCheckExtensions.cs`.

```csharp
services.AddHealthChecks()
    .AddCheck<YourNewHealthCheck>("your_check_name", tags: new[] { "your_tag" });
```

## Logging with Serilog

CoreAxis uses Serilog for structured logging. Serilog is configured in `Program.cs` and `serilog.json`.

### Log Enrichers

The following log enrichers are configured:

1. **FromLogContext**: Adds properties from the log context.
2. **WithMachineName**: Adds the machine name to log events.
3. **WithThreadId**: Adds the thread ID to log events.
4. **ModuleEnricher**: Adds information about registered modules to log events.

### Log Sinks

Logs are written to the following sinks:

1. **Console**: Logs are written to the console with a formatted output template.
2. **File**: Logs are written to files in the `logs` directory with a daily rolling interval.

### Customizing Logging

To customize logging, modify the `serilog.json` file. This file contains the configuration for Serilog, including minimum log levels, enrichers, and sinks.

## Best Practices

### Health Checks

1. **Keep health checks lightweight**: Health checks should be fast and not consume significant resources.
2. **Use appropriate tags**: Tags help categorize health checks and can be used to filter health checks for specific endpoints.
3. **Include relevant information in responses**: Health check responses should include information that helps diagnose issues.

### Logging

1. **Use structured logging**: Structured logging makes it easier to search and analyze logs.
2. **Include context in logs**: Add relevant context to logs to make them more useful for debugging.
3. **Use appropriate log levels**: Use the appropriate log level for each log message to avoid log noise.

## Future Enhancements

1. **Additional health checks**: Add health checks for databases, external services, etc.
2. **Metrics collection**: Implement metrics collection using Prometheus or similar tools.
3. **Distributed tracing**: Implement distributed tracing using OpenTelemetry or similar tools.
4. **Alerting**: Implement alerting based on health check results and log events.