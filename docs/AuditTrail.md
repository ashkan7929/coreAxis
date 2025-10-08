# Audit Trail (Wallet/Order/MLM)

Purpose
- Persist key audit events across Wallet, Order, and MLM modules with `correlationId` for end-to-end traceability.
- Ensure entries are queryable by `userId`, `orderId`, or `txId` via admin API.
- Prevent any PII from being logged in audit records.

Scope
- Wallet: `WalletTransaction` created/updated, balance changes, provider interactions.
- Order: `OrderStatusChange` (created -> paid -> shipped -> cancelled), failures.
- MLM: `CommissionSettlement`, plan assignment, payout approvals.

Audit Entry Model (suggested)
- `id` (Guid)
- `timestamp` (DateTimeOffset, UTC)
- `module` (string: Wallet|Order|MLM)
- `eventType` (string)
- `correlationId` (string)
- `userId` (Guid? nullable)
- `orderId` (Guid? nullable)
- `txId` (Guid? nullable)
- `severity` (Info|Warn|Error)
- `data` (JSON WITHOUT PII)

Admin API (spec)
- `GET /api/admin/audit?from&to&userId&orderId&txId&type&severity&page&size`
  - Returns JSON with pagination: `items`, `total`, `page`, `size`.
- `GET /api/admin/audit/export?format=csv|json&...same filters...`

Indexes (DB)
- Index on `correlationId`, `userId`, `orderId`, `txId`, `timestamp`.

Logging Rules
- Strip email/phone/address/credit-card; store only identifiers.
- Hash any free-form strings if needed for dedup (e.g., SHA-256).

Runbook Checks
- Verify entries for a test flow contain the same `correlationId`.
- Confirm admin API returns filtered results quickly (< 200ms p95 for 10k rows).