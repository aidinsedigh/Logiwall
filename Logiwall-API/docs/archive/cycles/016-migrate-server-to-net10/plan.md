# Cycle 016 — plan

## Contents

- L43 Git workflow
- L47 STOP conditions
- L55 Commands you will need
- L65 Tasks
  - L67 Task 1: Feasibility spike (TFM and SDK only)
  - L77 Task 2: Bump framework packages to 10.0.x
  - L89 Task 3: Bump third-party packages that have net9/10-aware majors
  - L98 Task 4: Fix obsolete and breaking APIs
  - L107 Task 5: Full test pass and runtime check
  - L114 Task 6: Infrastructure as code
- L122 Test plan
- L126 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/global.json "Logiwall-API/src/**/*.csproj" Logiwall-API/src/Directory.Build.props Logiwall-API/src/Logiwall.Iac/AppStack.cs`
> Plans 004 and 015 change these files on purpose. Confirm that `global.json` has
> `"rollForward": "latestFeature"` (from plan 004), and that the Server csproj no
> longer references Magick.NET or OData (from plan 015). If either is missing,
> STOP: run those plans first.
>
> **Amended 2026-10-27 — Central Package Management**: package versions now live in
> `Logiwall-API/Directory.Packages.props`. Apply these changes to the steps below:
> * Add `Logiwall-API/Directory.Packages.props` to the drift-check paths.
> * Change each package version in `Directory.Packages.props`, not in a `*.csproj`.
> * Some packages must differ between the net10 server projects and the net8 clients, for
>   example `Microsoft.AspNetCore.Components.WebAssembly*`. For those, use two `PackageVersion`
>   items with mutually exclusive `Condition`s on `$(TargetFramework)`, or a `VersionOverride`
>   in the one project.
> * After the change, run `dotnet list <project> package` for each project. Confirm that each
>   project resolves the version you expect.

## Git workflow

- Branch `advisor/016-net10-server`. Commit per step. Do NOT push.

## STOP conditions

- The .NET 10 SDK cannot build the net8 Blazor client projects with Bit 8.10 (Task 1). The options then are: (a) also migrate the clients to Bit 10 (a large, separate plan), or (b) decouple Server from `Client.Web` (an API-only decision). Both are product decisions, so report the exact error.
- Hosting the net8 WASM client from the net10 server fails at runtime.
- EF reports pending model changes, or a migration fails on a fresh DB in the integration tests.
- A third-party package has no version compatible with net10.
- Azure App Service doesn't offer `DOTNETCORE|10.0`.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Latest package version | `curl -s https://api.nuget.org/v3-flatcontainer/<lowercase-id>/index.json` | pick the highest stable `10.0.x` (or the latest stable major for third-party packages) |
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` | all pass |
| Pending model check | `cd src/Logiwall.Server && dotnet ef migrations has-pending-model-changes --project ../Logiwall.Infrastructure --startup-project .` | "No changes have been made to the model since the last migration." (the command exists in EF 8+) |
| Vulnerability audit | `dotnet list src/Logiwall.Server/Logiwall.Server.csproj package --vulnerable --include-transitive` | no High/Critical |

## Tasks

### Task 1: Feasibility spike (TFM and SDK only)

1. `global.json`: set `"version": "10.0.100"` (keep `"rollForward": "latestFeature"`, `"allowPrerelease": false`).
2. Change `<TargetFramework>net8.0</TargetFramework>` → `net10.0` in Domain, Application, Infrastructure, Server, Iac and both test projects.
   Don't touch the package versions yet.
3. `dotnet build Logiwall.Server.slnf`.

**Verify**: the build either succeeds, or fails **only** with package/TFM compatibility errors (NU1202 and similar) or obsolete-API warnings.
If it fails because `Client.Web`/`Client.Core` (net8.0, Bit 8.10) can't be built by the .NET 10 SDK, or because Bit source generators fail on net10 projects, **STOP** (see the STOP conditions).

### Task 2: Bump framework packages to 10.0.x

In the in-scope projects, set every `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`,
`Microsoft.Extensions.*` and `Microsoft.AspNetCore.Mvc.Testing` reference to the latest stable `10.0.x`.
Exception: `Microsoft.AspNetCore.Components.WebAssembly.Server` in Server. Try `10.0.x` first.
If hosting the net8 `Client.Web` then fails at build or run time, use the newest version that works, and report it.
Set `dotnet-ef` in `dotnet-tools.json` to the EF version.
Remove any explicit transitive pins that plan 004 added (they carry the comment `Pinned for CVE fix`),
then re-run the audit and re-add only those that are still needed.

**Verify**: build → exit 0 (warnings allowed).

### Task 3: Bump third-party packages that have net9/10-aware majors

`Serilog.AspNetCore` (and `Serilog.Sinks.File`), the `AspNetCore.HealthChecks.*` family, `Swashbuckle.AspNetCore`
and `Riok.Mapperly`: move each to its latest stable version. For each, read the package's release notes for breaking changes,
and record the version jump in your report. If `Swashbuckle.AspNetCore` conflicts with the ASP.NET 10 `Microsoft.OpenApi` version,
use the newest Swashbuckle version that builds.

**Verify**: build → exit 0; the audit command → no High or Critical.

### Task 4: Fix obsolete and breaking APIs

1. Replace both `new X509Certificate2(path, password, flags)` calls with
   `X509CertificateLoader.LoadPkcs12FromFile(path, password, flags)`.
2. Fix every **error** the build reports. For **warnings**, fix only `SYSLIB*` obsoletions in the in-scope projects.
3. Run the pending-model check. If it reports changes, **STOP**. Don't add a migration blindly.

**Verify**: build → exit 0; `dotnet build Logiwall.Server.slnf 2>&1 | grep -c "SYSLIB"` → 0 for the in-scope projects (client-project warnings may remain).

### Task 5: Full test pass and runtime check

1. `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` → all pass.
2. Run the server (`dotnet run --project src/Logiwall.Server`) and request `GET http://localhost:5030/` (the Blazor host page)
   and `GET http://localhost:5030/swagger/index.html` (Development) → both 200. If no local DB is available, the host page may
   error on data calls, but it must **start**. Report what you observed.

### Task 6: Infrastructure as code

In `src/Logiwall.Iac/AppStack.cs`, set `NetFrameworkVersion = "v10.0"` and `LinuxFxVersion = "DOTNETCORE|10.0"`.
Check that Azure App Service supports `DOTNETCORE|10.0` (the Azure docs, or `az webapp list-runtimes --os linux` if the Azure CLI is available).
If it doesn't, STOP.

**Verify**: `dotnet build src/Logiwall.Iac` → exit 0.

## Test plan

No new tests. The suites from plans 005–014 are the regression net and must all pass.

## Done criteria

- [ ] `dotnet --version` (from `Logiwall-API/`) → `10.0.x`
- [ ] `grep -rn "<TargetFramework>net8.0" src/Logiwall.Domain src/Logiwall.Application src/Logiwall.Infrastructure src/Logiwall.Server src/Logiwall.Iac tests` → no output
- [ ] `grep -rn "new X509Certificate2(" src/Logiwall.Server` → no output
- [ ] Build exits 0, and all tests pass
- [ ] The audit shows no High or Critical
- [ ] The pending-model check reports no changes
- [ ] `AppStack.cs` targets 10.0
- [ ] this cycle's `tasks.md` status row updated
