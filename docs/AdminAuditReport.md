# Admin Audit Report / Export

Endpoint
- `GET /api/admin/audit?from&to&userId&type&format=csv|json&page&size`
- `format` determines CSV vs JSON export.

Filters
- `from`, `to` (UTC); `userId`; `type` (eventType); `severity`.
- Pagination: `page` (1-based), `size` (default 50, max 500).

Responses
- JSON: `{ items: [], total, page, size }`
- CSV: header row, followed by entries; timestamps in ISO-8601.

CSV Rules
- No commas inside fields; escape with quotes if needed.
- Ensure `data` field is reduced (no PII); prefer summarized keys.

Validation
- Large-range pagination (>= 1M rows) should still stream efficiently.
- CSV lines validated and downloadable within < 10s for 100k rows.