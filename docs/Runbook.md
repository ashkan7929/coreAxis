# Ops Runbook

Purpose
- Practical guide for deployment, canary, rollback, monitoring and incident response.

Prerequisites
- CI/CD green on tag `v1.0.0`.
- Access to Grafana/Prometheus/Logs, DB and app servers.

Deployment
- Build and publish artifacts via CI.
- Apply migrations (see `scripts/ef-migrate.ps1`).
- Seed required data (see `scripts/seed-mlm.ps1`).

Canary
- Route 10% -> 50% -> 100% traffic.
- Gate: error rate < 0.1%, p95 latency under targets, no breaker storm.
- If gate fails, rollback.

Rollback
- Revert to previous deployment.
- Consider DB rollback only if backward-compatible; otherwise disable new paths via flags.
- Clear caches if needed.

Monitoring
- Dashboards: see `docs/GrafanaDashboards.md`.
- Alerts: see `docs/AlertRules.md`.

Incident Response
- Identify alert, open dashboard, check recent deploys and error logs.
- Correlate with `correlationId` in audit trail (`docs/AuditTrail.md`).
- Mitigate (feature-flag off / scale-out / rollback) and record timeline.

Verification
- Post-deploy smoke: health, key APIs, background jobs.
- Confirm alerts are active and dashboards show expected signals.