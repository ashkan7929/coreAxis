# ApiManager Module

ApiManager provides a centralized, policy-driven layer to register, secure, and invoke external web services with built-in resilience, observability, and governance.

## Scope
- Central registry of external services and their methods (paths and HTTP verbs)
- Pluggable security profiles (None, API Key, OAuth2) with key rotation policies
- Resilience via Polly (timeouts, retries, circuit breakers) per-method
- Unified invocation through an API proxy or runtime facade
- Rich call logging, problem details, and health/readiness probes

## Roadmap
- Multi-tenant scoping and per-tenant policy overrides
- Advanced auth flows (OAuth2 client credentials, token caching/refresh)
- OpenAPI import/export and schema validation of method parameters
- UI console for registry editing and live probing
- Quotas and rate-limiting per service and method

## Limitations
- Secrets must not be provided in import payloads; use secure provisioning
- Long-running downstream calls should be avoided or handled via async patterns
- Payload size limits and streaming support depend on downstream service capabilities
- Circuit and retry policies require careful tuning to avoid thundering herds

---

## Domain
- `WebService`: Name, `BaseUrl`, `Description?`, `SecurityProfileId?`, `IsActive`
- `WebServiceMethod`: `WebServiceId`, `Path`, `HttpMethod`, `TimeoutMs`, `RetryPolicyJson?`, `CircuitPolicyJson?`
- `WebServiceParam`: `MethodId`, `Name`, `Location` (Query, Header, Path, Body), `Type`, `IsRequired`, `DefaultValue?`
- `SecurityProfile`: `Type` (None, ApiKey, OAuth2), `ConfigJson`, `RotationPolicy?`
- `WebServiceCallLog`: `RequestDump`, `ResponseDump`, `StatusCode`, `LatencyMs`, `Succeeded`, `Error?`

## API

### Admin Services
- `GET /api/admin/apim/services` — List services (filter: `ownerTenantId`, `isActive`, `pageNumber`, `pageSize`)
- `GET /api/admin/apim/services/{id}` — Get service details
- `POST /api/admin/apim/services` — Create service
- `POST /api/admin/apim/services/{webServiceId}/methods` — Create method for service
- `POST /api/admin/apim/services/{webServiceId}/endpoints` — Alias for creating method
- `POST /api/admin/apim/services/methods/{methodId}/invoke` — Test/invoke method

### Registry
- `GET /api/apimanager/registry/export` — Export registry snapshot (JSON)
- `POST /api/apimanager/registry/import` — Import registry document (upsert services/methods)

### Security Profiles
- `GET /api/securityprofiles` — List profiles
- `GET /api/securityprofiles/{id}` — Get profile
- `POST /api/securityprofiles` — Create profile
- `PUT /api/securityprofiles/{id}` — Update profile
- `DELETE /api/securityprofiles/{id}` — Delete profile

### Logs
- `GET /api/admin/apim/logs` — Paged call logs (filter: `webServiceId`, `methodId`, `succeeded`, `fromDate`, `toDate`)

### Runtime Facade
- `POST /apim/call` — Invoke downstream by `methodId` or (`serviceName`, `httpMethod`, `path`)

### Health
- `GET /api/apimanager/health` — Health report
- `GET /api/apimanager/health/test-db` — DB connectivity test
- `GET /api/apimanager/health/ready?withProbes=true&maxProbeCount=3` — Readiness and optional probes

## Schema

### CreateWebServiceRequest
```json
{
  "name": "PricingService",
  "description": "Provides price quotes",
  "baseUrl": "https://api.example.com",
  "securityProfileId": "00000000-0000-0000-0000-000000000000",
  "ownerTenantId": "tenant-001"
}
```

### CreateWebServiceMethodRequest
```json
{
  "name": "GetQuote",
  "description": "Retrieve price quote",
  "path": "/quotes",
  "httpMethod": "GET",
  "timeoutMs": 3000,
  "retryPolicyJson": "{\"retryCount\":3}",
  "circuitPolicyJson": "{\"failureThreshold\":5}",
  "parameters": [
    { "name": "symbol", "location": "Query", "dataType": "string", "isRequired": true }
  ]
}
```

### InvokeApiMethodRequest
```json
{
  "parameters": { "symbol": "EURUSD" }
}
```

### RuntimeCallRequest
```json
{
  "serviceName": "PricingService",
  "httpMethod": "GET",
  "path": "/quotes",
  "parameters": { "symbol": "EURUSD" }
}
```

## Code Snippets

### Register module and Swagger XML comments
```csharp
services.AddApiManagerPresentation();
// Swagger in ApiGateway should call IncludeXmlComments for module assemblies
```

### Invoke via ApiProxy
```csharp
var result = await _apiProxy.InvokeAsync<QuoteDto>(
    serviceName: "PricingService",
    path: "/quotes",
    parameters: new Dictionary<string, object> { ["symbol"] = "EURUSD" }
);
```

### Runtime Facade call (HTTP)
```http
POST /apim/call
Content-Type: application/json

{
  "serviceName": "PricingService",
  "httpMethod": "GET",
  "path": "/quotes",
  "parameters": { "symbol": "EURUSD" }
}
```

## Configuration

- Connection string: `CoreAxis` SQL Server via EF Core
- Polly policies: JSON fields `retryCount`, `delay`, `failureThreshold`, etc.
- Rate limiting: Runtime facade protected by `apim-call` policy

## Monitoring
- Structured call logs with latency and status
- Health endpoints and optional synthetic probes
- ProblemDetails on errors with machine-readable codes

## Migration Commands
```bash
dotnet ef migrations add MigrationName --project src/Modules/ApiManager/Infrastructure/CoreAxis.Modules.ApiManager.Infrastructure
dotnet ef database update --project src/Modules/ApiManager/Infrastructure/CoreAxis.Modules.ApiManager.Infrastructure
```