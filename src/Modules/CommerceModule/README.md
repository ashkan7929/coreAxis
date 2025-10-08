# 🛒 Commerce Module

The Commerce Module provides end-to-end order, payment, inventory, pricing, and subscription capabilities for CoreAxis. This document mirrors the Wallet Module’s format, combining high-level context with detailed technical specifications and API samples.

---

## 🔹 Scope
- Order lifecycle: create, update, cancel, confirm, fulfill.
- Payments: process, refund, verify external status.
- Inventory: CRUD, reservations, release.
- Pricing: snapshot-based calculation and discounts.
- Subscriptions: create, update, cancel, pause/resume, billing.
- Consistent Problem+JSON errors and idempotency support where applicable.

## 🔹 Limitations
- External payment provider integrations are abstracted; concrete adapters vary by deployment.
- Advanced pricing rules and coupon engines are minimal; customize in `Application` layer.
- Inventory locations/warehouses are represented simply; multi-location orchestration is limited.

## 🔹 Roadmap
- Pluggable PSP integrations with webhooks (settlement and dispute flows).
- Rich discount engine with stacking, exclusivity, and targeting rules.
- Multi-warehouse inventory and reservation orchestration.
- Subscription proration, trial periods, and dunning management.

---

## 🔹 Domain
- Entities: `Order`, `OrderItem`, `Payment`, `Refund`, `InventoryItem`, `Reservation`, `Subscription`.
- Enums: `OrderStatus`, `PaymentStatus`, `RefundStatus`, `SubscriptionStatus`.
- Aggregates: Order (owns items, pricing summary), Subscription (billing lifecycle), InventoryItem (stock state).
- Events (conceptual): `OrderCreated`, `OrderConfirmed`, `OrderFulfilled`, `PaymentProcessed`, `RefundProcessed`, `SubscriptionBilled`.

---

## 🔹 API

All endpoints use REST conventions, return JSON, and document response codes. Errors use Problem+JSON (RFC 7807). For write operations that risk duplication, send `Idempotency-Key`.

### Orders
- `GET /api/v1/commerce/order` — List orders
  - Query: `customerId?`, `status?`, `fromDate?`, `toDate?`, `page?`, `pageSize?`
  - Responses: `200 OK`, `500 InternalServerError`

- `GET /api/v1/commerce/order/{id}` — Get order by id
  - Path: `id` (Guid)
  - Responses: `200 OK`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/order` — Create order
  - Body: `{ customerId, shippingAddress, billingAddress?, items: [{ productId, quantity, unitPrice }] }`
  - Responses: `201 Created`, `400 BadRequest`, `500 InternalServerError`

- `PUT /api/v1/commerce/order/{id}` — Update order
  - Body: `{ shippingAddress?, billingAddress?, status? }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/order/{id}/cancel` — Cancel order
  - Body: `{ reason }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/order/{id}/confirm` — Confirm order
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/order/{id}/fulfill` — Fulfill order
  - Body: `{ trackingNumber, shippingCarrier }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

### Payments
- `GET /api/v1/commerce/payment` — List payments
  - Query: `orderId?`, `customerId?`, `status?`, `fromDate?`, `toDate?`, `page?`, `pageSize?`
  - Responses: `200 OK`, `500 InternalServerError`

- `GET /api/v1/commerce/payment/{id}` — Get payment by id
  - Responses: `200 OK`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/payment/process` — Process payment
  - Body: `{ orderId, amount, paymentMethod, idempotencyKey? }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/payment/{paymentId}/refund` — Refund payment
  - Body: `{ amount, reason, idempotencyKey? }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `GET /api/v1/commerce/payment/{paymentId}/refunds` — List payment refunds
  - Responses: `200 OK`, `500 InternalServerError`

- `POST /api/v1/commerce/payment/{id}/verify` — Verify payment status
  - Responses: `200 OK`, `404 NotFound`, `500 InternalServerError`

### Inventory
- `GET /api/v1/commerce/inventory` — List inventory items
  - Query: `productId?`, `sku?`, `location?`, `lowStock?`, `page?`, `pageSize?`
  - Responses: `200 OK`, `500 InternalServerError`

- `GET /api/v1/commerce/inventory/{id}` — Get item by id
  - Responses: `200 OK`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/inventory` — Create item
  - Body: `{ productId, sku, quantityOnHand, reorderLevel }`
  - Responses: `201 Created`, `400 BadRequest`, `500 InternalServerError`

- `PUT /api/v1/commerce/inventory/{id}` — Update item
  - Body: `{ quantityOnHand?, reorderLevel?, maxStockLevel?, location? }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `DELETE /api/v1/commerce/inventory/{id}` — Delete item
  - Responses: `204 NoContent`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/inventory/reserve` — Reserve inventory
  - Body: `{ productId, quantity, customerId, reservationDurationMinutes }`
  - Responses: `200 OK`, `400 BadRequest`, `500 InternalServerError`

- `DELETE /api/v1/commerce/inventory/reservations/{reservationId}` — Release reservation
  - Responses: `204 NoContent`, `404 NotFound`, `500 InternalServerError`

### Pricing
- `POST /api/v1/commerce/pricing/calculate` — Calculate pricing
  - Body: `{ order: { items: [...] }, couponCodes?: [] }`
  - Responses: `200 OK`, `400 BadRequest`, `500 InternalServerError`

### Subscriptions
- `GET /api/v1/commerce/subscription` — List subscriptions
  - Query: `customerId?`, `status?`, `planId?`, `page?`, `pageSize?`
  - Responses: `200 OK`, `500 InternalServerError`

- `GET /api/v1/commerce/subscription/{id}` — Get subscription
  - Responses: `200 OK`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/subscription` — Create subscription
  - Body: `{ customerId, planId, startDate?, amount, billingCycle }`
  - Responses: `201 Created`, `400 BadRequest`, `500 InternalServerError`

- `PUT /api/v1/commerce/subscription/{id}` — Update subscription
  - Body: `{ planId?, amount?, billingCycle?, nextBillingDate? }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/subscription/{id}/cancel` — Cancel subscription
  - Body: `{ reason, cancelImmediately }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/subscription/{id}/pause` — Pause subscription
  - Body: `{ pauseUntil }`
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/subscription/{id}/resume` — Resume subscription
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

- `POST /api/v1/commerce/subscription/{id}/bill` — Process billing
  - Responses: `200 OK`, `400 BadRequest`, `404 NotFound`, `500 InternalServerError`

---

## 🔹 Schema

### DTOs (representative)
- `OrderDto`: `{ id, customerId, status, orderDate, shippingAddress, billingAddress, subtotalAmount, discountAmount, taxAmount, totalAmount, items[] }`
- `CreateOrderDto`: `{ customerId, shippingAddress, billingAddress?, items[] }`
- `UpdateOrderDto`: `{ shippingAddress?, billingAddress?, status? }`
- `PaymentDto`: `{ id, orderId, customerId, amount, paymentMethod, status, transactionId?, processedAt, idempotencyKey?, errorMessage? }`
- `ProcessPaymentDto`: `{ orderId, amount, paymentMethod, idempotencyKey? }`
- `RefundDto`: `{ id, paymentId, amount, reason, status, transactionId?, processedAt, idempotencyKey?, errorMessage? }`
- `ProcessRefundDto`: `{ amount, reason, idempotencyKey? }`
- `InventoryItemDto`: `{ id, productId, sku, quantityOnHand, quantityReserved, quantityAvailable, reorderLevel, maxStockLevel?, location?, lastUpdated }`
- `CreateInventoryItemDto`: `{ productId, sku, quantityOnHand, reorderLevel }`
- `UpdateInventoryItemDto`: `{ quantityOnHand?, reorderLevel?, maxStockLevel?, location? }`
- `ReserveInventoryDto`: `{ productId, quantity, customerId, reservationDurationMinutes }`
- `InventoryReservationDto`: `{ reservationId, productId, customerId, quantity, expiresAt, success, errorMessage? }`
- `PricingCalculateRequestDto`: `{ order: OrderSnapshotDto, couponCodes?: [] }`
- `PricingResultDto`: `{ success, errorMessage?, basePricing?, finalPricing?, appliedDiscounts[], validCoupons[] }`
- `SubscriptionDto`: `{ id, customerId, planId, status, startDate, endDate?, nextBillingDate?, amount, billingCycle, cancellationReason?, pauseUntil?, createdAt, updatedAt? }`
- `CreateSubscriptionDto`: `{ customerId, planId, startDate?, amount, billingCycle }`
- `UpdateSubscriptionDto`: `{ planId?, amount?, billingCycle?, nextBillingDate? }`
- `CancelSubscriptionDto`: `{ reason, cancelImmediately }`
- `PauseSubscriptionDto`: `{ pauseUntil }`

---

## 🔹 Code Snippets

### Register module (API layer)
```csharp
// Program.cs within CoreAxis.ApiGateway or the module host
builder.Services.AddSwaggerGen(c =>
{
    // XML comments from module assemblies should be added here
});
```

### Sample: Process Payment
```http
POST /api/v1/commerce/payment/process
Content-Type: application/json

{
  "orderId": "0d9e6bca-0000-0000-0000-000000000123",
  "amount": 250.00,
  "paymentMethod": "Card",
  "idempotencyKey": "123e4567-e89b-12d3-a456-426614174000"
}
```

Response (200 OK)
```json
{
  "id": "8c9a9f40-...",
  "orderId": "0d9e6bca-...",
  "status": "Completed",
  "amount": 250.00,
  "transactionId": "txn_123",
  "processedAt": "2025-01-01T10:00:00Z"
}
```

### Sample: Calculate Pricing
```http
POST /api/v1/commerce/pricing/calculate
Content-Type: application/json

{
  "order": {
    "orderId": "c0ffee00-...",
    "customerId": "1f3b3b3b-...",
    "subtotalAmount": 100.00,
    "currency": "USD",
    "items": [
      { "productId": "p-1001", "quantity": 2, "unitPrice": 50.00, "totalPrice": 100.00 }
    ]
  },
  "couponCodes": ["SPRING10"]
}
```

Response (200 OK)
```json
{
  "success": true,
  "basePricing": { "subtotalAmount": 100.00, "taxAmount": 8.00, "shippingAmount": 0.00, "totalAmount": 108.00 },
  "finalPricing": { "totalDiscountAmount": 10.00, "totalAmount": 98.00 }
}
```

### Sample: Reserve Inventory
```http
POST /api/v1/commerce/inventory/reserve
Content-Type: application/json

{
  "productId": "p-1001",
  "quantity": 3,
  "customerId": "1f3b3b3b-...",
  "reservationDurationMinutes": 15
}
```

Response (200 OK)
```json
{
  "reservationId": "res-00001",
  "success": true,
  "expiresAt": "2025-01-01T10:15:00Z"
}
```

---

## 🔹 Operational Notes
- Prefer consistent `ProblemDetails` for error responses in controllers.
- Use `Idempotency-Key` for payment/refund writes to avoid duplicate processing.
- Add `X-Correlation-ID` header to trace pricing calculations and long chains.