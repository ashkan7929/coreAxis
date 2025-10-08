# 🧩 Workflow Module

The Workflow Module orchestrates business workflows (e.g., order post-finalization) across CoreAxis services. It provides high-level documentation and detailed technical specs, aligned with Clean Architecture and modular composition.

---

## 🔹 Scope
- Orchestrate and signal long-running workflows (stateful, resumable).
- Entry points to start, resume, cancel, and inspect workflows.
- Persist minimal execution logs for diagnostics (NDJSON).
- Integrate with domain events (e.g., `OrderFinalized`).

## 🔹 Limitations
- No built-in distributed saga persistence; depends on workflow client/service.
- Logs are best-effort and may not capture external system outcomes.
- Payloads are generic `JSON` (no strict DTOs) to maximize integrability.

## 🔹 Roadmap
- Strongly-typed DTOs for common workflow payloads.
- Pluggable persistence for workflow states and checkpoints.
- Rich telemetry (OpenTelemetry spans) and correlation propagation.
- Admin APIs for listing workflows and advanced querying.

---

## 🔹 Domain
- Concepts: `Workflow`, `Signal`, `ExecutionStatus`, `HistoryEntry`.
- Events: subscribes to `OrderFinalized` to start post-finalize workflow.
- Policies: idempotent start by `workflowId` (handled by client/service layer), safe resume/cancel signaling.

---

## 🔹 API

Base route: `api/workflow`

### Start Post-Finalize
- `POST /api/workflow/post-finalize/start`
  - Body (JSON): arbitrary context for post-finalization, typically:
    ```json
    {
      "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "59e3c5f1-7f2d-4b22-9e8f-5c7c4e5b2a10",
      "totalAmount": 149.99,
      "currency": "USD",
      "finalizedAt": "2024-01-01T10:00:00Z",
      "tenantId": "coreaxis",
      "correlationId": "c2c2b2e2-..."
    }
    ```
  - Responses:
    - `200 OK` → `{ workflowId, isSuccess, error? }`
    - `400 BadRequest`, `401 Unauthorized`, `500 InternalServerError`

### Resume
- `POST /api/workflow/{workflowId}/resume`
  - Path: `workflowId` (Guid)
  - Responses:
    - `200 OK` → resume signal result
    - `404 NotFound`, `401 Unauthorized`, `500 InternalServerError`

### Cancel
- `POST /api/workflow/{workflowId}/cancel`
  - Path: `workflowId` (Guid)
  - Responses:
    - `200 OK` → cancel signal result
    - `404 NotFound`, `401 Unauthorized`, `500 InternalServerError`

### Get Status
- `GET /api/workflow/{workflowId}`
  - Path: `workflowId` (Guid)
  - Responses:
    - `200 OK` → status payload
    - `404 NotFound`, `401 Unauthorized`, `500 InternalServerError`

### Get History
- `GET /api/workflow/{workflowId}/history`
  - Path: `workflowId` (Guid)
  - Responses:
    - `200 OK` → `{ workflowId, entries: [...] }`
    - `404 NotFound`, `401 Unauthorized`, `500 InternalServerError`

---

## 🔹 Schema

### WorkflowStartResult
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isSuccess": true,
  "error": null
}
```

### SignalResult
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "signal": "Resume",
  "accepted": true,
  "details": null
}
```

### WorkflowStatus
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Running",
  "currentStep": "PostFinalize:Notify",
  "startedAt": "2024-01-01T10:00:00Z",
  "updatedAt": "2024-01-01T10:05:00Z",
  "metadata": { "tenantId": "coreaxis" }
}
```

### HistoryEntry
```json
{
  "timestamp": "2024-01-01T10:00:00Z",
  "level": "Information",
  "message": "Started post-finalize workflow",
  "data": { "orderId": "..." }
}
```

---

## 🔹 Code Snippets

### Register module (API layer)
```csharp
services.AddWorkflowModuleApi(configuration);
```

### Start Post-Finalize
```http
POST /api/workflow/post-finalize/start
Content-Type: application/json

{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "59e3c5f1-7f2d-4b22-9e8f-5c7c4e5b2a10",
  "totalAmount": 149.99,
  "currency": "USD",
  "finalizedAt": "2024-01-01T10:00:00Z"
}
```

Response (200 OK)
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isSuccess": true
}
```

### Resume
```http
POST /api/workflow/3fa85f64-5717-4562-b3fc-2c963f66afa6/resume
```

Response (200 OK)
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "signal": "Resume",
  "accepted": true
}
```

### Cancel
```http
POST /api/workflow/3fa85f64-5717-4562-b3fc-2c963f66afa6/cancel
```

Response (200 OK)
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "signal": "Cancel",
  "accepted": true
}
```

### Get Status
```http
GET /api/workflow/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Response (200 OK)
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Running"
}
```

### Get History
```http
GET /api/workflow/3fa85f64-5717-4562-b3fc-2c963f66afa6/history
```

Response (200 OK)
```json
{
  "workflowId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "entries": [
    { "timestamp": "2024-01-01T10:00:00Z", "level": "Information", "message": "Started post-finalize workflow" }
  ]
}
```

---

## 🔹 Operational Notes
- Workflow payloads are flexible JSON; validate required fields at the edge.
- Use `X-Correlation-ID` to trace workflow actions across services.
- History logs are stored under `App_Data/workflows/{workflowId}/logs.ndjson` when available.
- Signals (`Resume`, `Cancel`) are idempotent at the workflow engine level.