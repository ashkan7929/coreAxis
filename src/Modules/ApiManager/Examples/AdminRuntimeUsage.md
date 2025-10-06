# ApiManager Admin & Runtime Usage

This guide provides practical examples for registering services/endpoints, configuring policies and masking, and calling the runtime façade. Use these snippets to validate your setup quickly.

## Admin: Register Security Profile (no secrets in DB)

API Key profile (header-based). Store secret in an external secret manager; here we only reference a `SecretRef` key:

```bash
curl -X POST "https://localhost:5001/api/securityprofiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "securityType": "ApiKey",
    "configJson": "{\"headerName\":\"X-API-Key\",\"secretRef\":\"kv:apim:my-service:key\"}",
    "rotationPolicy": null
  }'
```

OAuth2 client credentials. Only `SecretRef` identifiers are stored (no client secret value):

```bash
curl -X POST "https://localhost:5001/api/securityprofiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "securityType": "OAuth2",
    "configJson": "{\"tokenUrl\":\"https://auth.example.com/oauth/token\",\"clientId\":\"svc-coreaxis\",\"clientSecretRef\":\"kv:apim:svc-coreaxis:secret\",\"scope\":\"api.read\"}",
    "rotationPolicy": null
  }'
```

Token caching behavior (client-credentials):

- Tokens are cached per `(tenantId, clientId, scope, tokenEndpoint)`.
- Preemptive refresh occurs if remaining lifetime is less than `preemptiveRefreshSeconds` (default 60s).
- Uses `IDistributedCache` when available; falls back to `IMemoryCache`.
- You can override the refresh threshold via `preemptiveRefreshSeconds` in the profile config:

```bash
curl -X POST "https://localhost:5001/api/securityprofiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "securityType": "OAuth2",
    "configJson": "{\"tokenUrl\":\"https://auth.example.com/oauth/token\",\"clientId\":\"svc-coreaxis\",\"clientSecretRef\":\"kv:apim:svc-coreaxis:secret\",\"scope\":\"api.read\",\"preemptiveRefreshSeconds\":90}"
  }'
```

To enable distributed caching (e.g., Redis), register `IDistributedCache` in your host app and the module will use it automatically:

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "apim:";
});
```

## Admin: Register HMAC Security Profile

HMAC signer with canonical string and optional body hash. You can provide `secret` directly or via `secretRef`. Clock-skew tolerance header defaults to ±300s and can be overridden.

```bash
curl -X POST "https://localhost:5001/api/securityprofiles" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "securityType": "HMAC",
    "configJson": "{\"headerName\":\"X-Signature\",\"timestampHeader\":\"X-Timestamp\",\"includeBodyHash\":true,\"bodyHashAlgorithm\":\"SHA256\",\"algorithm\":\"HMACSHA256\",\"secretRef\":\"kv:apim:svc-hmac:secret\",\"toleranceSeconds\":300,\"toleranceHeader\":\"X-Time-Tolerance\"}"
  }'
```

Canonical string format used for signature:
- METHOD (uppercased)
- PATH (e.g., `/v1/resource`)
- QUERY (raw query string without leading `?`)
- TIMESTAMP (Unix seconds)
- BODY_HASH (empty if not present)

Receiver validates signature and timestamp within tolerance; client sends `X-Timestamp`, `X-Time-Tolerance`, and `X-Signature` headers.

## Admin: Register Service and Endpoint

Register a web service:

```bash
curl -X POST "https://localhost:5001/api/webservices" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "name": "Users API",
    "baseUrl": "https://api.example.com",
    "description": "External users directory",
    "securityProfileId": "<profile-guid>"
  }'
```

Add an endpoint with policies and masking:

```bash
curl -X POST "https://localhost:5001/api/webservices/<service-id>/methods" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "path": "/v1/users/{id}",
    "httpMethod": "GET",
    "timeoutMs": 3000,
    "retryPolicyJson": "{\"retries\":2,\"baseDelayMs\":200,\"jitter\":true,\"cacheTtlSeconds\":30}",
    "circuitPolicyJson": "{\"failureThreshold\":5,\"breakDurationSeconds\":60}",
    "parameters": [
      { "name": "id", "location": "Path", "type": "int", "isRequired": true }
    ],
    "requestMask": ["Authorization"],
    "responseMask": ["token","cardNo"]
  }'
```

## Runtime: Call via Façade (`POST /apim/call`)

Generic dispatch with correlation and tenant headers:

```bash
curl -X POST "https://localhost:5001/api/apimanager/apim/call" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <user-token>" \
  -H "X-Correlation-Id: 00000000-0000-0000-0000-000000000001" \
  -H "X-Tenant-Id: tenant-1" \
  -d '{
    "serviceName": "Users API",
    "endpointPath": "/v1/users/{id}",
    "httpMethod": "GET",
    "parameters": { "id": 42 }
  }'
```

Rate limit 429 example (fixed-window):

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
Retry-After: 60

{
  "type": "https://coreaxis/errors/rate-limit",
  "title": "Rate limit exceeded",
  "status": 429,
  "detail": "Too many requests for tenant tenant-1",
  "instance": "/api/apimanager/apim/call"
}
```

## Health Readiness and Synthetic Probes

Check readiness; when probes are configured, they run within a small timeout budget:

```bash
curl "https://localhost:5001/api/apimanager/health/ready" -H "Authorization: Bearer <admin-token>"
```

Response examples:

```http
HTTP/1.1 200 OK
{ "ready": true, "probes": [ {"name":"Users API","status":"ok"} ] }
```

```http
HTTP/1.1 503 Service Unavailable
{ "ready": false, "errors": ["Db unreachable","Users API: timeout"] }
```

## Bulk Import/Export (no secrets)

Export registry (secrets sanitized):

```bash
curl "https://localhost:5001/api/apimanager/registry/export" -H "Authorization: Bearer <admin-token>"
```

Import registry (upsert semantics, secrets forbidden in payload):

```bash
curl -X POST "https://localhost:5001/api/apimanager/registry/import" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin-token>" \
  -d '{
    "webServices": [
      {
        "name": "Users API",
        "baseUrl": "https://api.example.com",
        "securityProfile": {
          "type": "OAuth2",
          "config": { "tokenUrl": "https://auth.example.com/oauth/token", "clientId": "svc-coreaxis" },
          "rotationPolicy": null
        },
        "methods": [
          { "path": "/v1/users/{id}", "httpMethod": "GET", "timeoutMs": 3000, "parameters": [{"name":"id","location":"Path","type":"int","isRequired":true}] }
        ]
      }
    ]
  }'
```

## ProblemDetails Samples

Masking violation:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://coreaxis/errors/masking",
  "title": "Invalid masking config",
  "status": 400,
  "detail": "responseMask contains unsupported field name",
  "instance": "/api/webservices/123/methods"
}
```

Endpoint not found:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://coreaxis/errors/not-found",
  "title": "Endpoint not found",
  "status": 404,
  "detail": "Users API /v1/users/{id} does not exist",
  "instance": "/api/apimanager/apim/call"
}
```

## Notes

- Response cache is enabled when `retryPolicyJson.cacheTtlSeconds` is set; non-idempotent methods bypass caching.
- OAuth2 tokens are cached per `(tenant, clientId, scope)` and refreshed at ~80% TTL.
- Outbox emits `ApiManager.RegistryImported.v1` on bulk registry import to help downstream caches.