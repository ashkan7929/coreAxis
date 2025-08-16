# ApiManager Module

ApiManager module provides a comprehensive solution for managing external API integrations with advanced features like circuit breakers, retry policies, and call logging.

## Features

- **Web Service Management**: CRUD operations for external API configurations
- **Method Configuration**: Define HTTP methods with custom parameters and policies
- **Security Profiles**: Support for API Key and OAuth2 authentication
- **Resilience Patterns**: Built-in circuit breaker and retry policies using Polly
- **Call Logging**: Comprehensive logging of all API calls with performance metrics
- **Test Runner**: Built-in API testing capabilities

## Architecture

```
ApiManager/
├── Domain/           # Domain entities and business logic
├── Application/      # Application services and use cases
├── Infrastructure/   # Data access and external integrations
├── Api/             # REST API controllers
└── Presentation/    # Shared presentation logic
```

## Domain Entities

### WebService
Represents an external API service configuration:
- **BaseUrl**: The base URL of the external service
- **Name**: Human-readable name for the service
- **Description**: Optional description
- **SecurityProfileId**: Reference to authentication configuration
- **IsActive**: Enable/disable the service

### WebServiceMethod
Defines specific API endpoints within a service:
- **Path**: The endpoint path (e.g., "/api/users/{id}")
- **HttpMethod**: GET, POST, PUT, DELETE, etc.
- **TimeoutMs**: Request timeout in milliseconds
- **RetryPolicyJson**: Polly retry policy configuration
- **CircuitPolicyJson**: Polly circuit breaker configuration

### SecurityProfile
Manages authentication configurations:
- **SecurityType**: None, ApiKey, OAuth2
- **ConfigJson**: Authentication-specific configuration
- **RotationPolicy**: Key rotation settings

### WebServiceCallLog
Tracks all API calls for monitoring and debugging:
- **RequestDump**: Full request details
- **ResponseDump**: Full response details
- **StatusCode**: HTTP status code
- **LatencyMs**: Request duration
- **Succeeded**: Success/failure flag
- **Error**: Error details if failed

## Usage Examples

### 1. Creating a Web Service

```csharp
// Create security profile
var securityProfile = new SecurityProfile(
    SecurityType.ApiKey,
    "{\"headerName\":\"X-API-Key\",\"keyValue\":\"your-api-key\"}",
    null
);

// Create web service
var webService = new WebService(
    "https://api.example.com",
    "Example API",
    "External service for data integration",
    securityProfile.Id
);

// Add HTTP method
var method = webService.AddMethod(
    "/api/users/{id}",
    "GET",
    5000, // 5 second timeout
    "{\"retryCount\":3,\"delay\":\"00:00:01\"}", // Retry policy
    "{\"failureThreshold\":5,\"duration\":\"00:01:00\"}" // Circuit breaker
);
```

### 2. Using ApiProxy Service

```csharp
public class ExampleService
{
    private readonly IApiProxy _apiProxy;
    
    public ExampleService(IApiProxy apiProxy)
    {
        _apiProxy = apiProxy;
    }
    
    public async Task<UserDto> GetUserAsync(int userId)
    {
        var parameters = new Dictionary<string, object>
        {
            { "id", userId }
        };
        
        var response = await _apiProxy.InvokeAsync<UserDto>(
            "Example API",
            "/api/users/{id}",
            parameters
        );
        
        return response.IsSuccess ? response.Value : null;
    }
}
```

### 3. Configuration in Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add ApiManager module
    services.AddApiManagerModule(Configuration);
    
    // Register your services
    services.AddScoped<ExampleService>();
}
```

## API Endpoints

### Web Services
- `GET /api/webservices` - List all web services
- `GET /api/webservices/{id}` - Get specific web service
- `POST /api/webservices` - Create new web service
- `PUT /api/webservices/{id}` - Update web service
- `DELETE /api/webservices/{id}` - Delete web service

### Web Service Methods
- `GET /api/webservices/{serviceId}/methods` - List methods for a service
- `POST /api/webservices/{serviceId}/methods` - Add new method
- `PUT /api/webservices/{serviceId}/methods/{methodId}` - Update method
- `DELETE /api/webservices/{serviceId}/methods/{methodId}` - Delete method

### Security Profiles
- `GET /api/securityprofiles` - List all security profiles
- `POST /api/securityprofiles` - Create new security profile
- `PUT /api/securityprofiles/{id}` - Update security profile
- `DELETE /api/securityprofiles/{id}` - Delete security profile

### Test Runner
- `POST /api/webservices/{serviceId}/methods/{methodId}/test` - Test specific method
- `POST /api/webservices/{serviceId}/test` - Test all methods in a service

### Call Logs
- `GET /api/calllogs` - List call logs with filtering
- `GET /api/calllogs/{id}` - Get specific call log

## Configuration

### Database
The module uses Entity Framework Core with the following connection string format:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreAxis;Trusted_Connection=true;"
  }
}
```

### Polly Policies

#### Retry Policy Example
```json
{
  "retryCount": 3,
  "delay": "00:00:01",
  "backoffType": "exponential"
}
```

#### Circuit Breaker Policy Example
```json
{
  "failureThreshold": 5,
  "duration": "00:01:00",
  "minimumThroughput": 10
}
```

## Security Considerations

1. **API Keys**: Store securely and rotate regularly
2. **OAuth2**: Implement proper token refresh mechanisms
3. **Logging**: Ensure sensitive data is not logged in call dumps
4. **Rate Limiting**: Configure appropriate limits to prevent abuse

## Monitoring and Observability

- All API calls are logged with correlation IDs
- Performance metrics are captured (latency, success rate)
- Circuit breaker state changes are logged
- Failed requests include detailed error information

## Testing

The module includes comprehensive tests:
- Unit tests for domain logic
- Integration tests for API endpoints
- Test utilities for mocking external services

Run tests with:
```bash
dotnet test Tests/CoreAxis.Tests/ --filter "FullyQualifiedName~ApiManager"
```

## Dependencies

- **Polly**: For resilience patterns
- **Entity Framework Core**: For data persistence
- **Microsoft.Extensions.Http**: For HTTP client factory
- **Serilog**: For structured logging

## Migration Commands

```bash
# Add new migration
dotnet ef migrations add MigrationName --project src/Modules/ApiManager/Infrastructure/CoreAxis.Modules.ApiManager.Infrastructure

# Update database
dotnet ef database update --project src/Modules/ApiManager/Infrastructure/CoreAxis.Modules.ApiManager.Infrastructure
```