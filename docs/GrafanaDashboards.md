# Grafana Dashboards (Final)

Panels
- Pricing latency: p95/p99 (`pricing_request_duration_seconds`)
- API Manager retry/breaker: retry count, breaker open rate (`apim_breaker_open_total`)
- Checkout success rate: 2xx/failed ratio (`checkout_success_ratio`)
- Wallet/MLM ops: transactions per minute, settlement durations.

Publishing
- Import dashboards JSONs to Grafana; folder `CoreAxis`.
- Tag panels with service/module.

Runbook Links
- Add links in Ops Runbook under "Monitoring" to published dashboards.
- Files: `monitoring/dashboards/pricing_latency.json`, `monitoring/dashboards/ops_overview.json`.

Validation
- Confirm all dashboards render and data sources connected.