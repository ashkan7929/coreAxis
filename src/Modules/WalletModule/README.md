# 📦 Wallet Module

The Wallet Module provides comprehensive wallet and transaction management for the CoreAxis platform. It documents high-level scope and roadmap alongside detailed technical specifications and API usage, following Clean Architecture.

---

## 🔹 Scope
- Multi-wallet per user with distinct `WalletType`s.  
- Core operations: `Deposit`, `Withdraw`, `Transfer`.  
- Balance queries, statements, and transaction history.  
- Admin capabilities: reconciliation, snapshots, lock/unlock.  
- Idempotent writes and RFC 7807 Problem+JSON error responses.  
- Optional correlation and rate-limiting headers on write endpoints.  

## 🔹 Limitations
- No direct bank/PSP integrations in-core; supported via extensions.  
- Crypto payments not implemented yet.  
- Multi-currency scalability requires stress testing for extreme throughput.  

## 🔹 Roadmap
- Crypto wallet integration and chain settlement.  
- PSP/bank settlement service integration.  
- Advanced fraud detection and anomaly scoring.  
- Profile-based multi-wallet orchestration and automated rules.  

---

## 🔹 Domain
- Entities: `Wallet`, `Transaction`, `WalletType`, `WalletContract`.  
- Aggregates: Wallet (owns transactions), Transaction (links to source/destination).  
- Events: `WalletCreated`, `WalletBalanceChanged`, `TransactionCreated`, `TransactionCompleted`, `TransactionFailed`.  
- Policies: contract-enforced limits, lock/unlock safety, optimistic concurrency.  

---

## 🔹 API

All endpoints return structured responses and Problem+JSON errors with `code`, descriptive `title`, and optional `traceId`/correlation extensions. For write operations, send `Idempotency-Key` to ensure safe retries.

### Wallet
- `POST /api/wallet` — Create wallet  
  - Body: `{ userId, walletType, currency, metadata? }`  
  - Responses: `201 Created` with wallet, `400 BadRequest`, `409 Conflict`  
  - Notes: supports idempotency via header.  

- `GET /api/wallet/{id}` — Get wallet by id  
  - Path: `id` (Guid)  
  - Responses: `200 OK`, `404 NotFound`  

- `GET /api/wallet/user/{userId}` — List user wallets  
  - Path: `userId` (Guid)  
  - Query: `type?`, `currency?`  
  - Responses: `200 OK`  

- `GET /api/wallet/{id}/balance` — Get balance  
  - Path: `id` (Guid)  
  - Responses: `200 OK` → `{ balance, currency }`, `404 NotFound`  

- `GET /api/wallet/statements` — Get statements (paged)  
  - Query: `userId?`, `walletId?`, `from?`, `to?`, `cursor?`, `limit?`  
  - Responses: `200 OK` → items+cursor, `400 BadRequest`  

### Transactions
- `POST /api/wallet/{id}/deposit` — Deposit funds  
  - Path: `id` (Guid)  
  - Body: `{ amount, description?, reference?, metadata? }`  
  - Headers: `Idempotency-Key` (GUID)  
  - Responses: `202 Accepted`, `201 Created` (idempotent), `400`, `409`, `429`  

- `POST /api/wallet/{id}/withdraw` — Withdraw funds  
  - Path: `id` (Guid)  
  - Body: `{ amount, description?, reference?, metadata? }`  
  - Headers: `Idempotency-Key`  
  - Responses: `202`, `400`, `409`, `422`, `429`  

- `POST /api/wallet/{id}/transfer` — Transfer to another wallet  
  - Path: `id` (Guid) — source wallet  
  - Body: `{ toWalletId, amount, description?, reference?, metadata? }`  
  - Headers: `Idempotency-Key`  
  - Responses: `202`, `400`, `404`, `409`, `422`, `429`  

- `GET /api/wallet/{id}/transactions` — List wallet transactions  
  - Path: `id` (Guid)  
  - Query: `from?`, `to?`, `type?`, `cursor?`, `limit?`  
  - Responses: `200 OK`  

### Transaction Queries
- `GET /api/transaction/{id}` — Get transaction by id  
  - Path: `id` (Guid)  
  - Responses: `200 OK`, `404 NotFound`  

- `GET /api/transaction` — Filtered transaction listing  
  - Query: `walletId?`, `userId?`, `type?`, `status?`, `from?`, `to?`, `cursor?`, `limit?`  
  - Responses: `200 OK`  

- `GET /api/transaction/user/{userId}` — User transactions  
  - Path: `userId` (Guid)  
  - Query: `type?`, `status?`, `from?`, `to?`, `cursor?`, `limit?`  
  - Responses: `200 OK`  

### Admin
- `GET /api/admin/wallet/reconcile` — Reconciliation overview  
  - Responses: `200 OK` → summary counters  

- `GET /api/admin/wallet/reports/reconciliation/export` — Export reconciliation  
  - Query: `format=csv|json`, `from?`, `to?`  
  - Responses: `200 OK` (file/JSON), `400 BadRequest`  

- `GET /api/admin/wallet/snapshot/{walletId}` — Cached balance snapshot  
  - Path: `walletId` (Guid)  
  - Responses: `200 OK`, `404 NotFound`  

- `POST /api/admin/wallet/{id}/lock` — Lock wallet  
  - Path: `id` (Guid)  
  - Body: `{ reason }`  
  - Responses: `200 OK`, `400`, `404`  

- `POST /api/admin/wallet/{id}/unlock` — Unlock wallet  
  - Path: `id` (Guid)  
  - Body: `{ reason }`  
  - Responses: `200 OK`, `400`, `404`  

---

## 🔹 Schema

### Core Entities
- `Wallet`: `{ id, userId, type, currency, balance, status, metadata }`  
- `Transaction`: `{ id, walletId, type, amount, status, createdAt, description?, reference?, metadata? }`  
- `WalletType`: `{ id, name, description?, isDefault }`  

### DTOs (common)
- `CreateWalletRequest`  
- `DepositRequest`, `WithdrawRequest`, `TransferRequest`  
- `TransactionResponse`, `WalletResponse`, `BalanceResponse`  
- `StatementPage<T>` with `items` and `cursor`  

---

## 🔹 Code Snippets

### Register module (API layer)
```csharp
services.AddWalletModuleApi(configuration);
```

### Sample: Deposit
```http
POST /api/wallet/0d9e6bca-0000-0000-0000-000000000123/deposit
Idempotency-Key: 123e4567-e89b-12d3-a456-426614174000
Content-Type: application/json

{
  "amount": 100.00,
  "description": "Top-up",
  "reference": "ORD-100045"
}
```

Response (202 Accepted)
```json
{
  "transactionId": "8c9a9f40-...",
  "status": "Pending",
  "amount": 100.00
}
```

### Sample: Transfer
```http
POST /api/wallet/0d9e6bca-0000-0000-0000-000000000111/transfer
Idempotency-Key: 123e4567-e89b-12d3-a456-426614174001
Content-Type: application/json

{
  "toWalletId": "0d9e6bca-0000-0000-0000-000000000999",
  "amount": 50.00,
  "description": "Move funds",
  "reference": "TR-1001",
  "metadata": { "source": "app" }
}
```

Problem+JSON (example)
```json
{
  "type": "https://coreaxis.dev/problems/insufficient-balance",
  "title": "Insufficient balance",
  "status": 422,
  "detail": "Wallet has 20.00; requires 50.00",
  "code": "INSUFFICIENT_BALANCE",
  "traceId": "00-...-..."
}
```

---

## 🔹 Operational Notes
- Always provide `Idempotency-Key` for write endpoints to avoid duplicate effects.  
- Use optional `X-Correlation-ID` for end-to-end tracing.  
- Prefer cursor pagination (`cursor`, `limit`) for long listings.