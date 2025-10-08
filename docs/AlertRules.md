# Alerts

Targets
- TTL Quote Expired spike
- Breaker-open spike
- Payment/settlement failures
- 429 anomaly

Examples (PromQL-like)
- TTL Quote Expired spike:
  - `sum(rate(quote_expired_total[5m])) > 50`
- Breaker-open spike:
  - `sum(rate(apim_breaker_open_total[5m])) > 5`
- Payment/settlement failures:
  - `sum(rate(payment_failure_total[5m])) > 10`
- 429 anomaly:
  - `sum(rate(http_429_total[5m])) > 20`

Runbook
- Each alert links to a diagnostic playbook.
- Test-fire alerts in staging; record screenshots and timestamps.