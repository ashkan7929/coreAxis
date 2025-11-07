# ProductOrder Module

This document provides a comprehensive overview and technical documentation for the ProductOrder module. It is structured similarly to `WalletModule/README.md`, including high-level scope, roadmap, limitations, and detailed technical specs: domain, APIs, schemas, and code snippets.

## Scope
- Manage public product catalog visibility and retrieval.
- Provide admin product management (create, update, delete) with authorization and idempotency.
- Handle customer order lifecycle: place, read, list (by user), cancel, and retrieve order lines.
- Emit domain events for product changes where applicable.

## Roadmap
- Introduce inventory and stock reservation during order placement.
- Add promotion/discount handling and tax calculation.
- Support order status transitions (Pending, Paid, Shipped, Canceled) and background workflows.
- Implement pagination metadata for product and order lists.
- Add soft-delete for products with restore capability.

## Limitations
- No built-in payment or shipment integration; handled externally.
- Order listing pagination is basic; metadata may be limited.
- Validation errors rely on FluentValidation rules within the application layer.
- Concurrency control depends on idempotency for create/update operations.

---

## Domain
- **Product**: represents an item with `code`, `name`, `status`, `price`, `currency`, and `attributes`.
- **Order**: aggregate containing `orderLines` (productId, quantity, unitPrice), `totalAmount`, and ownership by user.
- **Repositories/Services**: `IProductRepository`, `IOrderRepository`, `IIdempotencyService`, `IProductEventEmitter`.

---

## API

### Public Products
- `GET /api/products`
  - Returns a list of public products.
  - 200 OK: `ProductDto[]`
- `GET /api/products/{id}`
  - Returns product by ID.
  - 200 OK: `ProductDto`
  - 404 NotFound
- `GET /api/products/by-code/{code}`
  - Returns product by code.
  - 200 OK: `ProductDto`
  - 404 NotFound

### Admin Products
- `GET /api/admin/products/{id}`
  - Returns admin view of product.
  - 200 OK: `ProductAdminDto`
  - 404 NotFound
- `POST /api/admin/products`
  - Creates product (supports `Idempotency-Key` header for safe retries).
  - 201 Created: `ProductAdminDto` with `Location` header
  - 400 BadRequest (validation)
  - 409 Conflict (duplicate code)
- `PUT /api/admin/products/{id}`
  - Updates product.
  - 200 OK: `ProductAdminDto`
  - 400 BadRequest (validation)
  - 404 NotFound
- `DELETE /api/admin/products/{id}`
  - Deletes product.
  - 204 NoContent
  - 404 NotFound

### Orders
- `POST /api/order`
  - Places a new order (supports `Idempotency-Key`).
  - 201 Created: `OrderDto` with `Location` header
  - 400 BadRequest
  - 401 Unauthorized
- `GET /api/order/{id}`
  - Returns order by ID.
  - 200 OK: `OrderDto`
  - 404 NotFound
- `GET /api/order`
  - Returns current user's orders.
  - 200 OK: `OrderDto[]`
  - 401 Unauthorized
- `POST /api/order/{id}/cancel`
  - Cancels an order.
  - 204 NoContent
  - 404 NotFound
- `GET /api/order/{id}/lines`
  - Returns order line items.
  - 200 OK: `OrderLineDto[]`
  - 404 NotFound

---

## Schema

### Product Model Changes
- New field `count` (int): immutable baseline, does not decrease after purchase.
- Existing field `quantity` (decimal?): represents available stock and decreases after purchase.
- No changes to existing order placement or quantity reduction logic.

### ProductDto
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SKU-001",
  "name": "Product 1",
  "status": "Active",
  "price": 99.99,
  "currency": "USD",
  "attributes": { "color": "blue" }
}
```

### ProductAdminDto
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SKU-001",
  "name": "Product 1",
  "status": "Active",
  "price": 99.99,
  "currency": "USD",
  "attributes": { "color": "blue" },
  "createdAt": "2024-01-01T10:00:00Z",
  "updatedAt": "2024-01-02T10:00:00Z"
}
```

### CreateProductRequest
```json
{
  "code": "SKU-001",
  "name": "Product 1",
  "status": "Active",
  "priceFrom": 99.99,
  "currency": "USD",
  "attributes": { "color": "blue" }
}
```

### UpdateProductRequest
```json
{
  "name": "Product 1 (Updated)",
  "status": "Active",
  "priceFrom": 109.99,
  "currency": "USD",
  "attributes": { "color": "dark-blue" }
}
```

### OrderDto
```json
{
  "id": "e1f3ad8c-e37d-4d7f-a5c0-3b9c1f7a3d22",
  "assetCode": "PRD-001",
  "totalAmount": 149.99,
  "orderLines": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 2,
      "unitPrice": 74.995
    }
  ],
  "status": "Pending",
  "createdAt": "2024-01-01T10:00:00Z"
}
```

### OrderLineDto
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 2,
  "unitPrice": 74.995,
  "name": "Product 1"
}
```

### PlaceOrderDto
```json
{
  "assetCode": "PRD-001",
  "totalAmount": 149.99,
  "orderLines": [
    { "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 2, "unitPrice": 74.995 }
  ]
}
```

---

## Code Snippets

### Place Order (C# HttpClient)
```csharp
var client = new HttpClient { BaseAddress = new Uri("https://localhost:5016/") };
client.DefaultRequestHeaders.Add("Authorization", "Bearer <token>");
client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

var order = new
{
    assetCode = "PRD-001",
    totalAmount = 149.99,
    orderLines = new[]
    {
        new { productId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), quantity = 2, unitPrice = 74.995 }
    }
};

var response = await client.PostAsJsonAsync("api/order", order);
response.EnsureSuccessStatusCode();
var created = await response.Content.ReadFromJsonAsync<OrderDto>();
```

### Create Product (Admin, Idempotent)
```csharp
var client = new HttpClient { BaseAddress = new Uri("https://localhost:5016/") };
client.DefaultRequestHeaders.Add("Authorization", "Bearer <admin-token>");
client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

var create = new {
    code = "SKU-001",
    name = "Product 1",
    status = "Active",
    priceFrom = 99.99,
    currency = "USD",
    attributes = new { color = "blue" }
};

var response = await client.PostAsJsonAsync("api/admin/products", create);
response.EnsureSuccessStatusCode();
var product = await response.Content.ReadFromJsonAsync<ProductAdminDto>();
```

### Cancel Order
```csharp
var client = new HttpClient { BaseAddress = new Uri("https://localhost:5016/") };
client.DefaultRequestHeaders.Add("Authorization", "Bearer <token>");

var resp = await client.PostAsync($"api/order/{orderId}/cancel", null);
if (resp.StatusCode == HttpStatusCode.NoContent) {
    // canceled
}
```

---

## Operational Notes
- Swagger documentation is enhanced via XML comments; ensure `GenerateDocumentationFile` stays enabled in the API `.csproj`.
- Admin endpoints require appropriate permissions (e.g., `HasPermission("Products", "Write")`).
- Use `Idempotency-Key` for create/update operations to avoid duplicate processing.
- Exceptions are returned using Problem+JSON where applicable.

---

## Changelog
- Initial documentation added; aligned with controller annotations for Swagger.