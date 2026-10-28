# Cycle 004 — The backend builds on any current .NET 8 SDK and resolves no packages with known high or critical vulnerabilities: spec

## Contents

- L19 Summary
- L28 Why this matters
- L45 Repo facts you need
- L58 Current state
- L82 Scope
- L100 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 004 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P1 (time-critical: .NET 8 support ends on **2026-11-10**)
- **Effort**: S
- **Risk**: LOW (patch-level bumps inside 8.0.x)
- **Depends on**: none
- **Category**: migration
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

This is the stopgap to apply before the full .NET 10 migration (plan 016).
Today:
1. The committed `global.json` pins SDK `8.0.201` with `"rollForward": "disable"`.
   Fresh clones and CI machines without exactly that SDK cannot build. The
   developer machine has 8.0.301, which is why the file shows as locally modified.
2. Every `Microsoft.*` package is pinned to 8.0.8 (August 2024). Several
   resolved packages have published advisories:
   - `System.Text.Json` 8.0.4, explicitly pinned in `Logiwall.Shared.csproj:21`: CVE-2024-43485 (DoS), fixed in 8.0.5
   - `Microsoft.Extensions.Caching.Memory` 8.0.0: CVE-2024-43483
   - `System.Formats.Asn1` 8.0.0: CVE-2024-38095
   - `Azure.Identity` 1.10.3 and `Microsoft.Identity.Client` 4.56.0, pulled in by `Microsoft.Data.SqlClient` via EF Core SqlServer 8.0.8: CVE-2024-35255

This plan moves everything to the latest 8.0.x patch and lets the SDK roll
forward, without changing any target framework.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`, and commands run from `Logiwall-API/`.
- 9 projects; every TFM is `net8.0` (MAUI uses `net8.0-android` and similar).
- Build only `src/Logiwall.Server/Logiwall.Server.csproj`. It references
  Application, Infrastructure, Shared, Domain (transitively), Client.Web and
  Client.Core. **Never build `Logiwall.sln`, and never touch
  `src/Client/Logiwall.Client.Maui`**: MAUI needs mobile workloads and is out of
  scope.
- At planning time the developer's working tree had an **uncommitted** edit to
  `global.json` (8.0.201 → 8.0.301). Your branch starts from the committed version.
- Installed SDKs on the developer machine: 8.0.301, 9.0.101, 10.0.400.

## Current state

`global.json` (committed):
```json
{
    "sdk": {
        "version": "8.0.201",
		"rollForward": "disable"
    }
}
```

Package references pinned to `8.0.x` (Microsoft) that are in scope:
- `src/Logiwall.Server/Logiwall.Server.csproj`: lines 16, 33-35, 37, 41-45, 49 (all `8.0.8`). `AspNetCore.HealthChecks.*` (8.0.x, lines 52-56) are third-party; bump only if the audit flags them.
- `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj`: lines 10-14 (`8.0.8`), 15 (`Microsoft.Extensions.Http` `8.0.0`).
- `src/Logiwall.Domain/Logiwall.Domain.csproj:10`: `Microsoft.Extensions.Identity.Stores` `8.0.8`.
- `src/Logiwall.Shared/Logiwall.Shared.csproj`: lines 16-20 (Microsoft.* 8.0.x) and **line 21 `System.Text.Json` `8.0.4`**.
- `src/Client/Logiwall.Client.Core/Logiwall.Client.Core.csproj:32-34`, `src/Client/Logiwall.Client.Web/Logiwall.Client.Web.csproj:31-32`: `Microsoft.AspNetCore.Components.*` `8.0.8`.
- `src/Logiwall.Server/.config/dotnet-tools.json`: `dotnet-ef` `8.0.2`.

Do not change: the `Bit.*` packages (`8.10.0`; their major version moves with
plan 016), `Riok.Mapperly`, `Serilog*`, `RestSharp`, `Newtonsoft.Json`,
`Pulumi.AzureNative`, `Swashbuckle.AspNetCore`.

## Scope

**In scope**:
- `global.json`
- `src/Logiwall.Server/Logiwall.Server.csproj`
- `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj`
- `src/Logiwall.Domain/Logiwall.Domain.csproj`
- `src/Logiwall.Shared/Logiwall.Shared.csproj`
- `src/Client/Logiwall.Client.Core/Logiwall.Client.Core.csproj` (Microsoft.* versions only)
- `src/Client/Logiwall.Client.Web/Logiwall.Client.Web.csproj` (Microsoft.* versions only)
- `src/Logiwall.Server/.config/dotnet-tools.json`

**Out of scope**:
- Any `TargetFramework` change (plan 016).
- `src/Client/Logiwall.Client.Maui/**`.
- Removing unused packages (plan 015). Bump them like the rest; plan 015 deletes them.
- `Logiwall.sln` and `Logiwall.Web.slnf` (plan 005 adds a server-only solution filter).

## Maintenance notes

- This is a stopgap. After 2026-11-10, .NET 8 gets no security fixes, so plan
  016 (move to .NET 10 LTS) must follow soon.
- Each explicit transitive pin added in Task 4 should be removed when its parent package catches up. Reviewers should check that the comment is present.
- Consider Central Package Management (`Directory.Packages.props`) during plan 016, so that version bumps are a one-file change.
