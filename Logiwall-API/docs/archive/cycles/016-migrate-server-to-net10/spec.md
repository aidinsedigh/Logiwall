# Cycle 016 — The server-side projects run on .NET 10 LTS and production is configured for it: spec

## Contents

- L18 Summary
- L27 Why this matters
- L37 Repo facts you need
- L56 Scope
- L73 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 016 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2 (.NET 8 support ended 2026-11-10)
- **Effort**: L
- **Risk**: MED (framework major version; mixed-TFM hosting of the Blazor client)
- **Depends on**: docs/archive/cycles/004-patch-dotnet8-and-vulnerable-packages/, docs/archive/cycles/005-establish-test-and-ci-baseline/, docs/archive/cycles/015-remove-dead-code-and-template-leftovers/
- **Category**: migration
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

.NET 8 stops receiving security patches on 10 November 2026. This app is internet-facing and handles logins and payments.
.NET 9 reaches end of support on the same day, so the target is **.NET 10 LTS**
(supported until November 2028). This plan migrates only the **server-side
graph**: Domain, Application, Infrastructure, Server and the tests. The Blazor
and MAUI client projects stay on `net8.0` and Bit 8 for now. Moving them means
upgrading Bit.BlazorUI across about 22 razor files, which is a separate decision
that depends on whether those clients are kept at all.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- Installed SDKs on the dev machine: 8.0.301, 9.0.101, **10.0.400**. CI uses `actions/setup-dotnet` with `global-json-file`.
- Project graph: `Logiwall.Server` → Application, Infrastructure, Shared, **Client.Web**.
  `Client.Web` → `Client.Core` → Shared. Infrastructure → Application, Domain, Shared.
  A `net10.0` project can reference `net8.0` projects, so Shared, Client.Core and Client.Web stay on `net8.0`.
- `src/Directory.Build.props` sets `<LangVersion>12.0</LangVersion>` for every project. Keep it.
  Language upgrades are not part of this plan.
- Code that is obsolete on .NET 9+ (SYSLIB0057), the `X509Certificate2(path, password, flags)` constructor:
  - `src/Logiwall.Server/Program.Services.cs` (in `AddIdentity`; the line starting `var certificate = new X509Certificate2(certificatePath, ...`)
  - `src/Logiwall.Server/Controllers/Identity/IdentityController.cs` (in `GenerateJwtToken`)
  The replacement is `X509CertificateLoader.LoadPkcs12FromFile(path, password, flags)`.
- EF Core 9+ throws on `Migrate()` when the model has changes that no migration
  captures (`PendingModelChangesWarning`). The integration tests call
  `MigrateAsync`, so they will surface this.
- IaC: `src/Logiwall.Iac/AppStack.cs:131` has `NetFrameworkVersion = "v8.0"`, and `:133` has `LinuxFxVersion = "DOTNETCORE|8.0"`.
- Test harness: `tests/Logiwall.UnitTests`, `tests/Logiwall.IntegrationTests` (plan 005). Build with `Logiwall.Server.slnf`.

## Scope

**In scope**:
- `global.json`
- `src/Logiwall.Domain/Logiwall.Domain.csproj`, `src/Logiwall.Application/Logiwall.Application.csproj`, `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj`, `src/Logiwall.Server/Logiwall.Server.csproj`, `src/Logiwall.Iac/Logiwall.Iac.csproj`
- `tests/Logiwall.UnitTests/*.csproj`, `tests/Logiwall.IntegrationTests/*.csproj`
- `src/Logiwall.Server/.config/dotnet-tools.json` (dotnet-ef 10)
- The two X509 call sites
- `src/Logiwall.Iac/AppStack.cs` (lines 131 and 133 only)
- Any source file where the compiler reports a **breaking-change error** caused by the upgrade (keep each fix minimal, and list them in your report)

**Out of scope**:
- `src/Logiwall.Shared`, `src/Client/**` TFMs and Bit packages (they stay net8.0 / 8.10.0).
- `LangVersion` and new C# features.
- Refactors that aren't required by compile errors.
- Central Package Management. Recommended, but do it as a separate follow-up if wanted.

## Maintenance notes

- Client projects remain on .NET 8 and Bit 8. The WASM payload ships the .NET 8 runtime, which gets no more patches after 2026-11-10. Schedule the client decision soon: migrate to Bit 10, or drop the Blazor clients in favour of the React frontend.
- After this lands, consider `Directory.Packages.props` so that future bumps are a one-file change.
- Operator: deploy to a staging slot first. Watch startup logs for Data Protection and certificate loading.
