# Performance & Load Testing

Targets
- p95 < 300ms (pricing), < 800ms (checkout)
- 5xx < 0.1%

Tools
- k6 or JMeter; CI job for nightly baseline.

Scenarios
- Pricing surge; Checkout with payment gateway; APIM retry/breaker patterns; Wallet and MLM operations.

Reporting
- Publish dashboard snapshots; attach to release notes.