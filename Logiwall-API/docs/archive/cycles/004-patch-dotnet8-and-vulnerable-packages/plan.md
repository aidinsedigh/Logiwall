# Cycle 004 — plan

## Contents

- L41 Git workflow
- L47 STOP conditions
- L57 Commands you will need
- L66 Tasks
  - L68 Task 1: Let the SDK roll forward within .NET 8
  - L84 Task 2: Remove the redundant System.Text.Json pin
  - L92 Task 3: Bump Microsoft packages to the latest 8.0 patch
  - L110 Task 4: Clear the remaining transitive advisories
- L125 Test plan
- L132 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/global.json Logiwall-API/src/Directory.Build.props "Logiwall-API/src/**/*.csproj" Logiwall-API/src/Logiwall.Server/.config/dotnet-tools.json`
> Other plans may have added package references, e.g. plan 003 may have added
> `Microsoft.Extensions.Options.ConfigurationExtensions`. That is fine:
> include those in the bump. Anything else that contradicts the excerpts is a
> STOP condition.
>
> **Amended 2026-10-27 — Central Package Management**: package versions now live in
> `Logiwall-API/Directory.Packages.props`. Every `*.csproj` lost its `Version` attributes on
> purpose. Do not STOP for that diff. Apply these changes to the steps below:
> * Add `Logiwall-API/Directory.Packages.props` to the drift-check paths.
> * Change each version in `Directory.Packages.props`, not in a `*.csproj`. The csproj line
>   numbers below are out of date, but the package names are correct.
> * To remove `System.Text.Json`, delete its `PackageReference` in `Logiwall.Shared.csproj` and
>   its `PackageVersion` in `Directory.Packages.props`.
> * Run the `Version="8.0.8"` and `System.Text.Json` greps on `Directory.Packages.props` and
>   on `src --include=*.csproj`. Both must give no output.

## Git workflow

- Branch: `advisor/004-patch-dotnet8`
- Commit messages: e.g. `Roll SDK forward within .NET 8`, `Bump Microsoft packages to 8.0.<N>`.
- Do NOT push.

## STOP conditions

- The build fails after the bump with errors that appear in code, not project
  files. A patch release should not break the API, so report the error.
- Fixing an advisory needs a major-version change (for example `Microsoft.Data.SqlClient` 6.x).
- `dotnet --version` resolves to 9.x or 10.x after Task 1. That would mean
  `global.json` is not being picked up, for example because you ran the
  command from the wrong directory.
- nuget.org is unreachable. Report it, and do not guess version numbers.

## Commands you will need

| Purpose | Command | Expected on success |
|---|---|---|
| Latest 8.0 patch of a package | `curl -s https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore/index.json` (lower-case id) | JSON list of versions; pick the highest `8.0.N` with no `-preview`/`-rc` suffix |
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0 |
| Vulnerability audit | `dotnet list src/Logiwall.Server/Logiwall.Server.csproj package --vulnerable --include-transitive` | each project section prints "has no vulnerable packages given the current sources", or lists only `Low`/`Moderate` items |
| SDK in use | `dotnet --version` (from `Logiwall-API/`) | a `8.0.x` version |

## Tasks

### Task 1: Let the SDK roll forward within .NET 8

Replace `global.json` with:
```json
{
    "sdk": {
        "version": "8.0.301",
        "rollForward": "latestFeature",
        "allowPrerelease": false
    }
}
```
This accepts any installed 8.0 SDK at 8.0.301 or later and never selects 9.x or 10.x.

**Verify**: `dotnet --version` (from `Logiwall-API/`) → prints `8.0.` followed by a feature band ≥ 301.

### Task 2: Remove the redundant System.Text.Json pin

Delete line 21 of `src/Logiwall.Shared/Logiwall.Shared.csproj`
(`<PackageReference Include="System.Text.Json" Version="8.0.4" />`). On `net8.0`,
System.Text.Json ships with the shared framework.

**Verify**: build → exit 0.

### Task 3: Bump Microsoft packages to the latest 8.0 patch

1. Look up the highest stable `8.0.N` version for `microsoft.entityframeworkcore`
   and for `microsoft.aspnetcore.components.webassembly` (command in the table).
   Call them `EF8` and `ASP8`. They are normally the same number.
2. Set every in-scope `PackageReference` that is currently `8.0.8` to that
   version: EF Core packages → `EF8`, ASP.NET Core packages → `ASP8`.
3. For the `Microsoft.Extensions.*` references at `8.0.0`/`8.0.1`/`8.0.2`
   (`Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`,
   `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Configuration.Json`,
   and `Microsoft.Extensions.Options.ConfigurationExtensions` if plan 003 added it),
   look up each package's highest stable `8.0.x` and use it.
4. Set `dotnet-ef` in `src/Logiwall.Server/.config/dotnet-tools.json` to `EF8`.

**Verify**:
- `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` → exit 0, and no new error kinds.
- `grep -rn 'Version="8.0.8"' src --include=*.csproj | grep -v Maui` → no output.

### Task 4: Clear the remaining transitive advisories

Run the vulnerability audit command. For each **High** or **Critical** package
it reports:
- If the package is transitive, add an explicit `PackageReference` with the
  lowest fixed version (shown in the advisory URL, or the next patch on
  nuget.org) to the project that pulls it in. `Azure.Identity` and
  `Microsoft.Identity.Client` belong in
  `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj`, next to the
  EF Core SqlServer reference. Add the comment
  `<!-- Pinned for CVE fix; remove when Microsoft.Data.SqlClient brings a patched version. -->`.
- If the only fix is a different major version, don't pin it. Record it in your report instead.

**Verify**: the audit command → no `High` or `Critical` rows in any project section. `Moderate`/`Low` rows are acceptable but must be listed in your report. Build → exit 0.

## Test plan

There is no test project at this point; plan 005 adds one. Verification is
the build plus the audit. If a local SQL Server is configured, also run the
server (`dotnet run --project src/Logiwall.Server`) and confirm `GET
http://localhost:5030/swagger/index.html` returns 200. Report whether you ran it.

## Done criteria

- [ ] `global.json` has `"rollForward": "latestFeature"`, and `dotnet --version` prints 8.0.x
- [ ] `grep -rn "System.Text.Json" src --include=*.csproj` → no output
- [ ] `grep -rn 'Version="8.0.8"' src --include=*.csproj | grep -v Maui` → no output
- [ ] The audit command shows no High/Critical vulnerabilities
- [ ] Build exits 0
- [ ] Only in-scope files changed (`git status`)
- [ ] this cycle's `tasks.md` status row updated
