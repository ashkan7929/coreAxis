# ApiManager Setup Guide

This guide walks you through setting up the ApiManager module to integrate with external APIs.

## Step 1: Database Setup

First, ensure your database is set up with the ApiManager tables:

```bash
# Navigate to the Infrastructure project
cd src/Modules/ApiManager/Infrastructure/CoreAxis.Modules.ApiManager.Infrastructure

# Add migration (if not already done)
dotnet ef migrations add InitialApiManagerMigration

# Update database
dotnet ef database update
```

## Step 2: Configure Services

In your `Program.cs` or `Startup.cs`, add the ApiManager module:

```csharp
using CoreAxis.Modules.ApiManager;

var builder = WebApplication.CreateBuilder(args);

// Add ApiManager module
builder.Services.AddApiManagerModule(builder.Configuration);

// Add other services
builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline
app.UseRouting();
app.MapControllers();

app.Run();
```

## Step 3: Create Security Profile

Before creating web services, you need to set up authentication. Here's how to create different types of security profiles:

### API Key Authentication

```http
POST /api/securityprofiles
Content-Type: application/json

{
  "securityType": "ApiKey",
  "configJson": "{\"headerName\":\"X-API-Key\",\"keyValue\":\"your-api-key-here\"}",
  "rotationPolicy": null
}
```

### OAuth2 Authentication

```http
POST /api/securityprofiles
Content-Type: application/json

{
  "securityType": "OAuth2",
  "configJson": "{\"clientId\":\"your-client-id\",\"clientSecret\":\"your-client-secret\",\"tokenUrl\":\"https://api.example.com/oauth/token\",\"scope\":\"read\"}",
  "rotationPolicy": "{\"rotationIntervalDays\":30}"
}
```

### No Authentication

```http
POST /api/securityprofiles
Content-Type: application/json

{
  "securityType": "None",
  "configJson": "{}",
  "rotationPolicy": null
}
```

## Step 4: Create Web Service

Once you have a security profile, create a web service:

```http
POST /api/webservices
Content-Type: application/json

{
  "baseUrl": "https://api.binance.com",
  "name": "Binance API",
  "description": "Cryptocurrency exchange API for price data",
  "securityProfileId": "your-security-profile-id-here",
  "isActive": true
}
```

## Step 5: Add HTTP Methods

Add specific endpoints to your web service:

### Simple GET Method

```http
POST /api/webservices/{serviceId}/methods
Content-Type: application/json

{
  "path": "/api/v3/ticker/price",
  "httpMethod": "GET",
  "timeoutMs": 5000,
  "retryPolicyJson": "{\"retryCount\":3,\"delay\":\"00:00:01\",\"backoffType\":\"exponential\"}",
  "circuitPolicyJson": "{\"failureThreshold\":5,\"duration\":\"00:01:00\",\"minimumThroughput\":10}",
  "parameters": [
    {
      "name": "symbol",
      "parameterType": "Query",
      "isRequired": true,
      "defaultValue": null
    }
  ]
}
```

### POST Method with Body

```http
POST /api/webservices/{serviceId}/methods
Content-Type: application/json

{
  "path": "/api/v1/orders",
  "httpMethod": "POST",
  "timeoutMs": 10000,
  "retryPolicyJson": "{\"retryCount\":2,\"delay\":\"00:00:02\"}",
  "circuitPolicyJson": "{\"failureThreshold\":3,\"duration\":\"00:02:00\"}",
  "parameters": [
    {
      "name": "symbol",
      "parameterType": "Body",
      "isRequired": true,
      "defaultValue": null
    },
    {
      "name": "side",
      "parameterType": "Body",
      "isRequired": true,
      "defaultValue": null
    },
    {
      "name": "type",
      "parameterType": "Body",
      "isRequired": true,
      "defaultValue": "MARKET"
    },
    {
      "name": "quantity",
      "parameterType": "Body",
      "isRequired": true,
      "defaultValue": null
    }
  ]
}
```

### Method with Path Parameters

```http
POST /api/webservices/{serviceId}/methods
Content-Type: application/json

{
  "path": "/api/v1/users/{userId}/orders/{orderId}",
  "httpMethod": "GET",
  "timeoutMs": 5000,
  "retryPolicyJson": "{\"retryCount\":3,\"delay\":\"00:00:01\"}",
  "circuitPolicyJson": "{\"failureThreshold\":5,\"duration\":\"00:01:00\"}",
  "parameters": [
    {
      "name": "userId",
      "parameterType": "Path",
      "isRequired": true,
      "defaultValue": null
    },
    {
      "name": "orderId",
      "parameterType": "Path",
      "isRequired": true,
      "defaultValue": null
    }
  ]
}
```

## Step 6: Test Your Configuration

Use the built-in test runner to verify your setup:

```http
POST /api/webservices/{serviceId}/methods/{methodId}/test
Content-Type: application/json

{
  "parameters": {
    "symbol": "BTCUSDT"
  }
}
```

## Step 7: Use in Your Application

Inject and use the `IApiProxy` service in your application:

```csharp
public class PriceService
{
    private readonly IApiProxy _apiProxy;
    
    public PriceService(IApiProxy apiProxy)
    {
        _apiProxy = apiProxy;
    }
    
    public async Task<decimal?> GetBitcoinPriceAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            { "symbol", "BTCUSDT" }
        };
        
        var response = await _apiProxy.InvokeAsync<PriceResponse>(
            "Binance API",
            "/api/v3/ticker/price",
            parameters
        );
        
        return response.IsSuccess ? response.Value?.Price : null;
    }
}

public class PriceResponse
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
}
```

## Step 8: Monitor and Debug

### View Call Logs

```http
GET /api/calllogs?webServiceId={serviceId}&succeeded=true&limit=50
```

### Check Circuit Breaker Status

Monitor your logs for circuit breaker state changes:

```
[Information] Circuit breaker opened for WebService: Binance API, Method: /api/v3/ticker/price
[Information] Circuit breaker half-opened for WebService: Binance API, Method: /api/v3/ticker/price
[Information] Circuit breaker closed for WebService: Binance API, Method: /api/v3/ticker/price
```

### Performance Monitoring

Call logs include performance metrics:
- **LatencyMs**: Request duration
- **StatusCode**: HTTP response status
- **Succeeded**: Success/failure flag
- **Error**: Error details if failed

## Common Configuration Examples

### High-Frequency Trading API

```json
{
  "timeoutMs": 1000,
  "retryPolicyJson": "{\"retryCount\":1,\"delay\":\"00:00:00.100\"}",
  "circuitPolicyJson": "{\"failureThreshold\":10,\"duration\":\"00:00:30\",\"minimumThroughput\":50}"
}
```

### Batch Processing API

```json
{
  "timeoutMs": 30000,
  "retryPolicyJson": "{\"retryCount\":5,\"delay\":\"00:00:05\",\"backoffType\":\"exponential\"}",
  "circuitPolicyJson": "{\"failureThreshold\":3,\"duration\":\"00:05:00\",\"minimumThroughput\":5}"
}
```

### Public API (Rate Limited)

```json
{
  "timeoutMs": 10000,
  "retryPolicyJson": "{\"retryCount\":3,\"delay\":\"00:00:02\",\"backoffType\":\"linear\"}",
  "circuitPolicyJson": "{\"failureThreshold\":5,\"duration\":\"00:02:00\",\"minimumThroughput\":10}"
}
```

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Verify API keys are correct
   - Check token expiration for OAuth2
   - Ensure proper header names for API key auth

2. **Timeout Issues**
   - Increase `timeoutMs` for slow APIs
   - Check network connectivity
   - Monitor external service status

3. **Circuit Breaker Tripping**
   - Review failure threshold settings
   - Check external service health
   - Adjust minimum throughput if needed

4. **Parameter Mapping Issues**
   - Verify parameter names match API documentation
   - Check parameter types (Query, Path, Body, Header)
   - Ensure required parameters are provided

### Debugging Tips

1. **Enable Detailed Logging**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "CoreAxis.Modules.ApiManager": "Debug"
       }
     }
   }
   ```

2. **Use Test Runner**
   - Test individual methods before using in code
   - Verify parameter mapping
   - Check response format

3. **Monitor Call Logs**
   - Review failed requests
   - Check response dumps for API changes
   - Monitor performance trends

## Security Best Practices

1. **API Key Management**
   - Store keys securely (Azure Key Vault, AWS Secrets Manager)
   - Rotate keys regularly
   - Use least privilege principle

2. **Network Security**
   - Use HTTPS only
   - Implement IP whitelisting if possible
   - Consider VPN for sensitive APIs

3. **Monitoring**
   - Set up alerts for high failure rates
   - Monitor for unusual usage patterns
   - Log security events

4. **Data Protection**
   - Avoid logging sensitive data
   - Implement data retention policies
   - Consider data encryption at rest