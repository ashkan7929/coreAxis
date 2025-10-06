# Product & Order Module (ProductOrderModule)

Purpose
- Provide REST APIs for managing products (admin/public) within ProductOrderModule.
- Emit optional integration events for product updates and status changes via Outbox.
- Keep boundaries with SharedKernel (ProblemDetails, Correlation, Outbox, EventBus).

Entities
- Product: Id (Guid), Code (string, unique), Name (string), Status (Active/Inactive), PriceFrom (Money), Attributes (Dictionary<string,string>), CreatedAt/UpdatedAt.

APIs
- Base route: `/api/products` (public)
  - GET `/api/products` — Paged active products with `q`, `status`, `sort` filters.
  - GET `/api/products/{id}` — Get product by id.
  - GET `/api/products/by-code/{code}` — Get product by code.

- Base route: `/api/admin/products` (admin, RBAC enforced)
  - POST `/api/admin/products` — Create product.
  - PUT `/api/admin/products/{id}` — Update product (name, priceFrom, status, attributes). Emits optional events.
  - DELETE `/api/admin/products/{id}` — Delete/deactivate product.
  - GET `/api/admin/products/{id}` — Get product by id (full admin DTO).

Events (optional, behind feature flag)
- ProductUpdated
  - Properties: `ProductId`, `Code`, `Name`, `PriceFrom { amount, currency }`, `Attributes`, `Status`, `CorrelationId`, `CausationId`, `TenantId`
- ProductStatusChanged
  - Properties: `ProductId`, `Code`, `OldStatus`, `NewStatus`, `CorrelationId`, `CausationId`, `TenantId`
- Publishing: via `IProductEventEmitter` using Outbox (`OutboxMessage`) when `ProductEvents.Enabled = true`.

Setup Instructions
- Composition Root (Web API): call `AddProductOrderModuleApi(builder.Configuration);` in `Program.cs`.
- Project reference: add `CoreAxis.Modules.ProductOrderModule.Api/CoreAxis.Modules.ProductOrderModule.Api.csproj` to Web API `.csproj`.
- Configuration:
  - `ProductEvents:Enabled` (bool) — enable optional product integration events.
  - Ensure SharedKernel services registered (ProblemDetails, Correlation, Outbox).

Example Requests/Responses
- Admin Update Product (PUT `/api/admin/products/{id}`)
  Request:
  ```json
  {
    "name": "New Name",
    "priceFrom": { "amount": 199.99, "currency": "USD" },
    "status": "Active",
    "attributes": { "color": "red" },
    "emitEvents": true
  }
  ```
  Response: `200 OK` with updated admin DTO, or RFC7807 Problem+JSON on error.

- Public List (GET `/api/products`)
  Response:
  ```json
  {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "code": "SKU-001",
        "name": "Product 1",
        "priceFrom": { "amount": 99.99, "currency": "USD" },
        "status": "Active",
        "attributes": { "color": "blue" }
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
  ```

Error Model
- All endpoints return RFC7807 Problem+JSON for errors.
- Responses include `X-Correlation-Id` when correlation is enabled.

Dependencies
- CoreAxis.SharedKernel, CoreAxis.BuildingBlocks, CoreAxis.EventBus
- Microsoft.EntityFrameworkCore (SQL Server), Swashbuckle (Swagger)

Notes
- No UI; REST-only. No breaking changes to existing Order handlers.