# Swagger & curl Cheatsheet

## Admin Audit
- `GET /api/admin/audit?page=1&size=50`
- Export CSV: `GET /api/admin/audit/export?format=csv&page=1&size=1000`

## APIM Logs
- `GET /admin/apim/logs?service=Pricing&from=...&to=...`

## Wallet & MLM
- Wallet accounts: `GET /api/wallet/accounts`
- MLM commissions: `GET /api/mlm/commissions`

## curl Examples
```bash
curl -H "Authorization: Bearer <token>" http://localhost:5016/api/admin/audit?page=1&size=50
curl -H "Authorization: Bearer <token>" http://localhost:5016/api/admin/audit/export?format=csv
```