MLM Module

Overview

- Scope: Provides multi-level marketing capabilities including user referral graph, commission rules, and commission transactions with admin actions (approve/reject/mark-paid).
- Roadmap: Dynamic tiered rules per product, versioned rule sets with audit trail, bulk admin operations, CSV export improvements, Grafana dashboards for MLM KPIs.
- Limitations: Assumes upstream Auth and Wallet services are available; rate-limited admin actions; background processing depends on database connectivity.

Domain

- User Referrals: Maintains parent-child relationships, supports upline/downline queries, and network statistics.
- Commission Rules: Defines commission rule sets with levels, percentages, and constraints; can bind rule sets to products.
- Commission Transactions: Tracks commission lifecycle per user (pending, approved, rejected, paid) with summaries and filtering.

API

- Commission Rules (`/api/CommissionRule`)
  - POST `/` — Create rule set; returns `CommissionRuleSetDto` (201).
  - GET `/{id}` — Get by id; returns `CommissionRuleSetDto` (200/404).
  - GET `/default` — Get default rule set (200/404).
  - GET `/product/{productId}` — Get rule set bound to product (200/404).
  - GET `/active` — List active rule sets (200).
  - GET `/` — List all rule sets (200).
  - PUT `/{id}` — Update rule set; returns `CommissionRuleSetDto` (200/400).
  - POST `/{id}/activate` — Activate; returns `CommissionRuleSetDto` (200).
  - POST `/{id}/deactivate` — Deactivate; returns `CommissionRuleSetDto` (200).
  - POST `/{id}/set-default` — Set as default (200).
  - DELETE `/{id}` — Delete (204).
  - GET `/product-bindings` — List product bindings (200).
  - GET `/product-bindings/product/{productId}` — List bindings by product (200).
  - POST `/product-bindings` — Create product binding; returns `ProductRuleBindingDto` (201/400).

- Commission Transactions (`/api/CommissionTransaction`)
  - GET `/{id}` — Get by id; returns `CommissionTransactionDto` (200/404).
  - GET `/user/{userId}` — List by user with paging (200).
  - GET `/status/{status}` — List by status with paging (200).
  - GET `/source-payment/{sourcePaymentId}` — List by source payment id (200).
  - GET `/user/{userId}/summary` — Summary totals for user (200).
  - GET `/pending-approval` — List pending (200).
  - GET `/date-range?startDate&endDate` — List by date range (200).
  - POST `/{id}/approve` — Approve; returns `CommissionTransactionDto` (200/429).
  - POST `/{id}/reject` — Reject; returns `CommissionTransactionDto` (200/429).
  - POST `/{id}/mark-paid` — Mark as paid; returns `CommissionTransactionDto` (200/429).
  - GET `/admin/report?format=json|csv` — Admin report (200).

- User Referrals (`/api/UserReferral`)
  - POST `/` — Create referral; returns `UserReferralDto` (201/400).
  - GET `/{id}` — Get by id; returns `UserReferralDto` (200/404).
  - GET `/user/{userId}` — Get by user id (200/404).
  - GET `/{userId}/children` — List children (200).
  - GET `/{userId}/upline?levels` — List upline up to levels (200).
  - GET `/{userId}/downline?levels` — List downline up to levels (200).
  - GET `/{userId}/network-stats` — Network stats (200).
  - GET `/{userId}/network-tree?maxDepth` — Network tree (200).
  - PUT `/{id}` — Update referral; returns `UserReferralDto` (200/400).
  - POST `/{userId}/activate` — Activate (200).
  - POST `/{userId}/deactivate` — Deactivate (200).
  - DELETE `/{id}` — Delete (204).

Schema

- CommissionRuleSetDto
  - `id`, `name`, `description`, `maxLevels`, `minimumPurchaseAmount`, `requireActiveUpline`, `isActive`, `commissionLevels[] { level, percentage }`.
- ProductRuleBindingDto
  - `productId`, `commissionRuleSetId`, `validFrom`, `validTo`.
- CommissionTransactionDto
  - `id`, `userId`, `amount`, `status`, `sourcePaymentId`, `createdAt`, `updatedAt`.
- CommissionSummaryDto
  - `totalAmount`, `approvedAmount`, `pendingAmount`, `rejectedAmount`, `paidAmount`, `count`.
- UserReferralDto
  - `id`, `userId`, `parentUserId`, `isActive`, `createdAt`.
- MLMNetworkStatsDto
  - `userId`, `totalDownline`, `activeDownline`, `levels`, `depth`.
- NetworkTreeDto
  - `rootUserId`, `maxDepth`, `nodes[] { userId, parentUserId, level, isActive }`.

Code Snippets

- Register module (auto-discovered via `IModule`):
  - The API Gateway discovers `MLMModule` implementing `IModule` and calls `RegisterServices` and `ConfigureApplication` to add controllers, rate limiting, and event subscriptions.

- Sample: Create a commission rule set
```
POST /api/CommissionRule
Content-Type: application/json

{
  "name": "Standard Plan",
  "description": "Default commission plan",
  "maxLevels": 3,
  "minimumPurchaseAmount": 100,
  "requireActiveUpline": true,
  "commissionLevels": [
    { "level": 1, "percentage": 10 },
    { "level": 2, "percentage": 5 },
    { "level": 3, "percentage": 2 }
  ]
}
```

- Sample: Approve a commission
```
POST /api/CommissionTransaction/{id}/approve
Content-Type: application/json

{
  "approvalNotes": "Reviewed and approved"
}
```

- Sample: Create user referral
```
POST /api/UserReferral
Content-Type: application/json

{
  "userId": "a1b2c3d4-...",
  "parentUserId": "p9q8r7s6-..."
}
```

Notes

- Admin actions are protected by rate limiting policy `mlm-actions`.
- Swagger documentation is enriched via XML comments; ensure `CoreAxis.Modules.MLMModule.API.xml` is present in gateway output.
- Hosted services and outbox processing may log database errors if the DB is unavailable; configure `ConnectionStrings` accordingly.