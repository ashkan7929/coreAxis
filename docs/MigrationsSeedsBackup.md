# Migrations / Seeds / Backup & Restore

Migrations
- `dotnet ef database update` per module/startup project.
- Validate schema diffs; store migration IDs.

Seeds
- Seed MLM default plan, product bindings, wallet policies via a seed script or startup flag `--seed-data`.

Backup
- SQL Server: full backup before cutover; verify backup completion.

Restore Test
- Restore into staging snapshot; run smoke tests; record success.

Verification
- "Migrations green" and "Backup restore passes" documented with timestamps.