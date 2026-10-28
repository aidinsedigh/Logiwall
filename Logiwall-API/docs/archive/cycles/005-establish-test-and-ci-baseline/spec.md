# Cycle 005 — `dotnet test` exercises the real API against real SQL Server, and CI runs it on every push: spec

## Contents

- L19 Summary
- L28 Why this matters
- L38 Repo facts you need
- L81 Current state
- L110 Scope
- L129 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 005 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P1 (prerequisite: every later risky plan needs a verification baseline)
- **Effort**: M
- **Risk**: LOW (adds projects; two one-line production changes)
- **Depends on**: docs/archive/cycles/001-remove-shared-default-password/, docs/archive/cycles/002-enforce-resource-ownership/ (their regression tests are written here and must pass), docs/archive/cycles/004-patch-dotnet8-and-vulnerable-packages/ (test packages must match its 8.0.x versions)
- **Category**: tests
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

The repo has **no test projects and no CI**. The core flows (OTP login, order
quote → select → pay, customer contacts) have never been checked
automatically, and nothing even confirms that the code compiles on push. Plans
011 and 012 rewrite money-handling code, and they need tests that can be run
before and after. This plan builds the harness with the smallest number of
production changes, and adds the first regression tests for the security fixes
from plans 001 and 002.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`, which contains `Logiwall-API/`
  (this backend) and `Logiwall-Front/` (React; don't touch it). Paths are
  relative to `Logiwall-API/` unless they start with the git root.
- .NET 8, ASP.NET Core with minimal hosting. `Program` is declared as
  `public static partial class Program` (`src/Logiwall.Server/Program.cs:7`). A
  static class cannot be the `TEntryPoint` of `WebApplicationFactory`, so use
  **`WebApplicationFactory<Logiwall.Server.AppSettings>`** (`AppSettings` is a
  public class in the Server assembly).
- **The database must be real SQL Server.** EF InMemory and SQLite do not work:
  - `AppDbContext.cs:43-45` defines the SQL sequence `dbo.OrderSequence`.
  - `OrderConfiguration.cs:11`: `HasDefaultValueSql("'L' + FORMAT(NEXT VALUE FOR dbo.OrderSequence, 'D6')")`.
  - Filtered unique indexes `[IsArchive] = 0` (migration `20240309114326_AlterIndexes.cs`).
  - Lookup data (cities, `AppSettings` rows, `ServiceProviders`) is seeded by raw SQL in migrations.

  So use **Testcontainers for SQL Server**, and call `Database.MigrateAsync()`
  once per test collection.
- Auth: `POST api/Identity/SendOtpCode` sends an SMS through `ISMSService`
  (`src/Logiwall.Application/Abstractions/Tools/ISMSService.cs`:
  `string SendSMS(string Message, string Destination)`), then
  `POST api/Identity/ConfirmationOtpCode` returns `{ data: { token } }`.
  The SMS text is the OTP code, or `LoginOtpText` with `{CODE}` replaced. Tests
  must replace `ISMSService` with a fake that records messages, and pull the
  code out with the regex `\d{5,6}`.
- Every API response is `ServiceResponse<T>`: `{ "data": ..., "succuss": bool, "message": string }`.
  `succuss` is a misspelling and part of the contract.
- Development configuration: the server only accepts the committed
  `IdentityCertificate.pfx` when the environment is `Development`
  (`Program.Services.cs:170-174`). Tests must run with environment `Development`.
- The cert is loaded from `Directory.GetCurrentDirectory()` at
  `Program.Services.cs:167` and `IdentityController.cs:306`. In a test run, the
  current directory is not the Server folder, so this plan switches both to
  `AppContext.BaseDirectory`. The `.pfx` is already copied to the output
  folder (`Logiwall.Server.csproj:69-71`, `CopyToOutputDirectory=Always`).
- `Program.cs:40-43` has `catch (Exception exc)`, which also swallows the
  `HostAbortedException` that `WebApplicationFactory` and `dotnet ef` use to
  stop the host after it is built.
- Build only the Server project or the new test projects. **Never build
  `Logiwall.sln` as a whole** (MAUI).
- Controllers live under `src/Logiwall.Server/Controllers/**`, with route
  `api/[controller]/[action]`.

## Current state

`src/Logiwall.Server/Program.cs:16-44` (abridged):
```csharp
try
{
    Log.Information("Starting web host");
    var builder = WebApplication.CreateBuilder(args);
    ...
    await app.RunAsync();
}
catch (Exception exc)
{
    Log.Fatal(exc, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

`src/Logiwall.Server/Program.Services.cs:167`:
`var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");`

`src/Logiwall.Server/Controllers/Identity/IdentityController.cs:306`:
`var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");`

There is no `tests/` folder and no `.github/` folder, either in `Logiwall-API/` or at the git root.

## Scope

**In scope**:
- `src/Logiwall.Server/Program.cs` (the catch filter only)
- `src/Logiwall.Server/Program.Services.cs` (line 167 only)
- `src/Logiwall.Server/Controllers/Identity/IdentityController.cs` (line 306 only)
- `tests/Logiwall.UnitTests/**` (create)
- `tests/Logiwall.IntegrationTests/**` (create)
- `Logiwall.Server.slnf` (create)
- `Logiwall.sln` (add the two test projects via `dotnet sln add`)
- `D:\Own Projects\Logiwall\.github\workflows\api-ci.yml` (create; note that this is at the **git root**)

**Out of scope**:
- Any other production code. If a test fails because of a real bug, don't fix
  the bug here. Mark that test `[Fact(Skip = "Known bug: <one line> — see docs/work/001-audit-remediation/NNN")]`
  and list it in your report.
- `Logiwall.Web.slnf` (it includes MAUI; leave it alone).
- Coverage tooling, mutation testing, snapshot testing.

## Maintenance notes

- Every later plan adds tests to these projects. Keep one container per
  collection, and isolate tests with fresh random phone numbers rather than
  by cleaning the DB.
- If tests become slow, add Respawn to reset data between collections. For now
  that isn't needed.
- When plan 008 changes `ISMSService` to async, it must update `FakeSmsService` too.
- The CI workflow lives at the git root because `.github/` must be at the
  repository root. The frontend has no CI yet; that is deliberately out of scope.
