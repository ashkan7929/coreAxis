# Cutover / Canary / Rollback

Canary Plan
- 10% -> 50% -> 100% traffic progression.
- Metrics gates: error rate < 0.1%, p95 latency under target, no breaker storm.

Feature Flags
- Flags for MLM/workflow jobs; enable progressively; rollback by flag off.

Rollback Drill
- Document steps to revert deployment, database migration rollback (if safe), and cache reset.
- Run drill; capture artifacts and pass criteria.

Checklist
- Pre-cutover checklist completed
- Canary metrics reviewed at each step
- Rollback path validated