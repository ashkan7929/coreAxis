# Requires: DotNet EF Tools installed
param(
    [string]$Env = "Staging",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# NOTE: Update these paths to your actual csproj locations
$migrations = @(
    @{ Name = "AuthModule"; Infra = "src/Modules/AuthModule/Infrastructure/CoreAxis.Modules.AuthModule.Infrastructure.csproj"; Startup = "src/Web/CoreAxis.Web.Api/CoreAxis.Web.Api.csproj" },
    @{ Name = "CommerceModule"; Infra = "src/Modules/CommerceModule/Infrastructure/CoreAxis.Modules.CommerceModule.Infrastructure.csproj"; Startup = "src/Web/CoreAxis.Web.Api/CoreAxis.Web.Api.csproj" },
    @{ Name = "WalletModule"; Infra = "src/Modules/WalletModule/Infrastructure/CoreAxis.Modules.WalletModule.Infrastructure.csproj"; Startup = "src/Web/CoreAxis.Web.Api/CoreAxis.Web.Api.csproj" },
    @{ Name = "MLMModule"; Infra = "src/Modules/MLMModule/Infrastructure/CoreAxis.Modules.MLMModule.Infrastructure.csproj"; Startup = "src/Web/CoreAxis.Web.Api/CoreAxis.Web.Api.csproj" }
)

Write-Host "Running EF migrations for environment: $Env" -ForegroundColor Cyan

foreach ($m in $migrations) {
    Write-Host "Updating database for $($m.Name)" -ForegroundColor Yellow
    dotnet ef database update --project $m.Infra --startup-project $m.Startup
}

Write-Host "Migrations completed." -ForegroundColor Green