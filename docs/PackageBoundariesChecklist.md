# CoreAxis Package Boundaries Checklist

This checklist helps keep SharedKernel decoupled from feature modules and enforce clean boundaries.

- SharedKernel must not reference any projects under `src/Modules/`.
- Allowed project references: `src/EventBus/CoreAxis.EventBus/`, BCL/ASP.NET Core packages, and other building blocks when truly cross-cutting.
- Check `.csproj` for `<ProjectReference>` elements pointing to `src/Modules/` paths — they should not exist.
- Avoid importing module-specific namespaces in SharedKernel. Move cross-cutting types to SharedKernel if needed.
- When adding new contracts, ensure they are generic and not coupled to a specific module.
- CI suggestion (optional): add a step to scan `.csproj` files and fail if a `<ProjectReference>` contains `/Modules/`.

Example PowerShell snippet for CI:

```powershell
Get-ChildItem -Recurse src\BuildingBlocks\SharedKernel -Filter *.csproj |
  ForEach-Object {
    $path = $_.FullName
    $content = Get-Content $path -Raw
    if ($content -match '<ProjectReference Include=".*\\Modules\\') {
      Write-Error "Cross-module reference found in $path"; exit 1
    }
  }
```

Document violations in PRs and relocate types accordingly.