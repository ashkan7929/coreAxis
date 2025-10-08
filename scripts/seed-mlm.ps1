param(
    [string]$BaseUrl = "http://localhost:5077",
    [string]$AuthToken = "",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Seed default MLM plan, product bindings, wallet policies via API endpoints
# NOTE: Replace endpoints and payloads with actual ones

function Invoke-Api($method, $url, $body) {
    $headers = @{ "Authorization" = "Bearer $AuthToken"; "Content-Type" = "application/json" }
    if ($Verbose) { Write-Host "[$method] $url" -ForegroundColor Cyan }
    if ($body) { $json = ($body | ConvertTo-Json -Depth 10) } else { $json = $null }
    return Invoke-RestMethod -Method $method -Uri $url -Headers $headers -Body $json
}

# Example seeds
Invoke-Api POST "$BaseUrl/api/mlm/plans" @{ name = "Default"; levels = 3; commission = 0.1 }
Invoke-Api POST "$BaseUrl/api/mlm/product-bindings" @{ productId = "00000000-0000-0000-0000-000000000000"; plan = "Default" }
Invoke-Api POST "$BaseUrl/api/wallet/policies" @{ name = "StandardPolicy"; dailyLimit = 10000; currency = "IRR" }

Write-Host "Seed operations submitted." -ForegroundColor Green