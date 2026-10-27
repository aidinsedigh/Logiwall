# Plan 015: Unused packages, dead code and misleading template namespaces are gone, so the code shows only what actually runs

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src`
> Many earlier plans touch `src/`. Run this plan **after** plans 001–014 have
> landed or been marked REJECTED/BLOCKED in `docs/work/001-audit-remediation/tasks.md`. Each step below
> starts with a grep that re-establishes the current state. Trust the grep, not
> line numbers.
>
> **Amended 2026-10-27 — Central Package Management**: package versions now live in
> `Logiwall-API/Directory.Packages.props`. To remove a package, delete its `PackageReference`
> from the `*.csproj`. If no other project uses it, also delete its `PackageVersion` from
> `Directory.Packages.props`. Run the done-criteria package grep on `Directory.Packages.props`
> too.

## Status

- **Priority**: P3
- **Effort**: S–M (mechanical; compiler-verified)
- **Risk**: LOW
- **Depends on**: docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md, docs/work/001-audit-remediation/008-fix-otp-send-path-and-async-sms.md (removes the last Newtonsoft use), docs/work/001-audit-remediation/012-verify-payments-server-side.md (makes `PaymentRequestDto` unused). Run it after all of plans 001–014.
- **Category**: tech-debt
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

The solution came from the bit-bp template and still carries things that look
live but aren't:
- NuGet packages nobody uses: Magick.NET (native image libraries), EF InMemory, EF Sqlite, MimeTypeMap, RestSharp, Newtonsoft. They add publish size, CVE surface and upgrade work for plan 016.
- An OData pipeline with no OData endpoints.
- About 190 lines of commented-out email/refresh/reset-password code, with five injected fields that exist only for it.
- A `RequestResponseLoggingMiddleware` that would log passwords and break streaming if anyone re-enabled it.
- Infrastructure types in `Logiwall.Server.*` namespaces, which makes Infrastructure *look* like it depends on Server.

Each item misleads both readers and agents. Removing them is compiler-verified.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- Build with `dotnet build Logiwall.Server.slnf` (plan 005). Never build `Logiwall.sln` (MAUI).
- `AppDbContext` is in `src/Logiwall.Infrastructure/Data/AppDbContext.cs` but declares `namespace Logiwall.Server.Data;`.
  `src/Logiwall.Infrastructure/Mapper/IdentityMapper.cs` declares `namespace Logiwall.Server.Mappers;`.
  The other mappers use `Logiwall.Infrastructure.Mapper`. At planning time, about 32 files referenced
  these namespaces: 22 in `Data/Migrations`, and the rest in Infrastructure services plus
  `src/Logiwall.Server/Logiwall.Server.csproj` lines 62–63 (`<Using Include="Logiwall.Server.Data" />`, `<Using Include="Logiwall.Server.Mappers" />`).
- EF identifies migrations by the `[Migration("<id>")]` attribute, not by namespace. Changing the
  `using` lines in migration files is safe. **Never change migration IDs, class names or `Up`/`Down` bodies.**
- OData: `Logiwall.Server.csproj:36` (package), `:66` (global using `Microsoft.AspNetCore.OData.Query`),
  `Program.Services.cs` (`using Microsoft.AspNetCore.OData;`, `.AddOData(options => options.EnableQueryFeatures())`,
  `options.OperationFilter<ODataOperationFilter>();`), and `src/Logiwall.Server/Services/ODataOperationFilter.cs`.
  At planning time no controller used `[EnableQuery]` or `ODataQueryOptions`.
- **Keep** FluentEmail (`Program.Services.cs` registration and the SMTP health check use the email settings). Whether email features come back is undecided.
- **Keep** `AppControllerBase.DbContext`: plan 011 uses it for the Register transaction.
- **Do not touch** `src/Client/**` (the Blazor/MAUI clients). Whether they stay is an open product decision.
  As a result, the Products/Categories/Dashboard DTOs in Shared, which the client widgets use, **stay**.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` | all pass |
| Migrations still discovered | `cd src/Logiwall.Server && dotnet ef migrations list --project ../Logiwall.Infrastructure --startup-project . --no-connect` | lists every migration, with the same count before and after (record the count before Step 4) |

## Scope

**In scope**:
- `src/Logiwall.Server/Logiwall.Server.csproj`, `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj`
- `src/Logiwall.Server/Program.Services.cs`, `Program.Middlewares.cs`, `Program.cs` (rename of `ConfiureMiddlewares`)
- `src/Logiwall.Server/Services/ODataOperationFilter.cs` (delete)
- `src/Logiwall.Server/Middleware/RequestResponseLoggingMiddleware.cs` (delete)
- `src/Logiwall.Server/Controllers/Identity/IdentityController.cs` (commented code and unused fields)
- `src/Logiwall.Infrastructure/**/*.cs` (namespace rename and `using` updates only, outside the explicit deletions below)
- `src/Logiwall.Shared/Dtos/PaymentItems/PaymentRequestDto.cs` and `src/Logiwall.Infrastructure/Mapper/PaymentMapper.cs` (delete if unused)
- `src/Logiwall.Application/Abstractions/Providers/PaymentGateways/IPostalServiceBase.cs` (namespace fix, or delete if unused)
- `src/Logiwall.Infrastructure/Mapper/Readme.md` (fix the stale sentence)

**Out of scope**: `src/Client/**`; FluentEmail; the `ServiceResponse.Succuss` spelling; the
`DashboardController` (the client calls it); Central Package Management (plan 016).

## Git workflow

- Branch `advisor/015-remove-dead-code`. **One commit per step** (so each can be reverted alone). Do NOT push.

## Steps

### Step 1: Remove unused packages

For each package below, first run the confirming grep. Remove the `PackageReference`
**only** if the grep returns nothing:

| Package (project) | Confirming grep (must be empty) |
|---|---|
| `Magick.NET-Q16-AnyCPU` (Server) | `grep -rn "ImageMagick\|MagickImage" src --include=*.cs` |
| `MimeTypeMapOfficial` (Server) | `grep -rn "MimeTypeMap" src --include=*.cs` |
| `Microsoft.EntityFrameworkCore.InMemory` (Server) | `grep -rn "UseInMemoryDatabase" src --include=*.cs` |
| `Microsoft.EntityFrameworkCore.Sqlite` (Server) | `grep -rn "UseSqlite" src --include=*.cs` |
| `RestSharp` (Infrastructure) | `grep -rn "RestSharp\|RestClient" src --include=*.cs` |
| `Newtonsoft.Json` (Infrastructure) | `grep -rn "Newtonsoft\|JsonConvert" src/Logiwall.Infrastructure src/Logiwall.Server --include=*.cs` |
| `Serilog.Extensions.Hosting`, `Serilog.Extensions.Logging` (Server) | none needed; both are dependencies of `Serilog.AspNetCore` |

**Verify**: build → exit 0; tests pass.

### Step 2: Remove the OData pipeline

Confirm first: `grep -rn "EnableQuery\|ODataQueryOptions\|MapODataRoute\|AddRouteComponents" src --include=*.cs` → only `ODataOperationFilter.cs`.
Then remove: the package reference, the global `<Using Include="Microsoft.AspNetCore.OData.Query" />`,
`using Microsoft.AspNetCore.OData;` and `.AddOData(...)` in `Program.Services.cs`,
`options.OperationFilter<ODataOperationFilter>();`, and delete `ODataOperationFilter.cs`.

**Verify**: build → exit 0; tests pass. If Swagger is enabled in Development, `GET /swagger/v1/swagger.json` still returns 200 (add a quick integration test assertion or check manually; report which).

### Step 3: Delete dead and dangerous code

1. Delete `src/Logiwall.Server/Middleware/RequestResponseLoggingMiddleware.cs` and the commented
   `//app.UseRequestResponseLoggingMiddleware();` line in `Program.Middlewares.cs`. Check first
   that nothing calls it: `grep -rn "UseRequestResponseLoggingMiddleware" src` → only that comment.
2. `IdentityController.cs`: delete every commented-out method block (`//[HttpPost]` through the
   end of that commented method: SendConfirmationEmail, ConfirmEmail, Refresh, SendResetPasswordEmail, ResetPassword),
   and the commented two-factor block inside `GetToken`. Then delete each `[AutoInject]` field that
   is now unused. Check each one with `grep -n "<fieldName>" IdentityController.cs` (candidates: `fluentEmail`,
   `emailLocalizer`, `htmlRenderer`, `bearerTokenOptions`, `dataProtectionProvider`), and remove the now-unused `using`s.
3. Rename `ConfiureMiddlewares` → `ConfigureMiddlewares` (the definition in `Program.Middlewares.cs` and the call in `Program.cs`).
4. If `grep -rn "PaymentRequestDto" src --include=*.cs` shows only its own file and `PaymentMapper.cs`,
   delete both files. Also remove any `[JsonSerializable(typeof(PaymentRequestDto))]` in `AppJsonContext.cs`, if present.
5. `IPostalServiceBase`: if `grep -rn "IPostalServiceBase" src --include=*.cs` shows only its declaration and
   `PostalServiceBase`'s base list, remove it from the base list and delete the interface file. Otherwise,
   move its namespace from `...Providers.PaymentGateways` to `...Providers` and fix the usings.

**Verify** after each sub-step: build → exit 0. At the end: tests pass.

### Step 4: Namespace rename

1. Record the migration count (`dotnet ef migrations list ... --no-connect`, see the commands table).
2. Change `namespace Logiwall.Server.Data;` → `namespace Logiwall.Infrastructure.Data;` in `AppDbContext.cs`,
   and `namespace Logiwall.Server.Mappers;` → `namespace Logiwall.Infrastructure.Mapper;` in `IdentityMapper.cs`.
3. Replace `using Logiwall.Server.Data;` → `using Logiwall.Infrastructure.Data;` and
   `using Logiwall.Server.Mappers;` → `using Logiwall.Infrastructure.Mapper;` everywhere under `src/` and `tests/`.
   In `Logiwall.Server.csproj`, update the two `<Using Include=... />` lines. If a file now has two identical
   `using Logiwall.Infrastructure.Mapper;` lines, keep one.
4. Fix the stale sentence in `src/Logiwall.Infrastructure/Mapper/Readme.md` that says mappers are called in controllers. They are called in Infrastructure services.

**Verify**: `grep -rn "Logiwall.Server.Data\|Logiwall.Server.Mappers" src tests` → no output; build → exit 0; the migration count is unchanged; all tests pass.

## Test plan

No new behavior, so no new tests. The full existing suite is the safety net and must pass after every step.

## Done criteria

- [ ] Build exits 0, and all unit and integration tests pass
- [ ] `grep -rn "Magick\|RestSharp\|Newtonsoft\|AspNetCore.OData\|EntityFrameworkCore.InMemory\|EntityFrameworkCore.Sqlite" src --include=*.csproj` → no output
- [ ] `grep -rn "Logiwall.Server.Data\|Logiwall.Server.Mappers\|ConfiureMiddlewares" src tests` → no output
- [ ] `ls src/Logiwall.Server/Middleware/RequestResponseLoggingMiddleware.cs` → not found
- [ ] The migration count is unchanged
- [ ] `git status` shows no changes under `src/Client/`
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- Any confirming grep returns a real usage. Keep that package, and report it.
- Removing a package breaks the **client** build (`Client.Web` is built as part of the server). That means a client project relied on it transitively. Keep it, and report.
- The migration count changes, or `dotnet ef` reports a model change.

## Maintenance notes

- Decision pending (product): keep the Blazor/MAUI clients and template dashboard, or go API-only.
  If API-only, a follow-up plan should remove `src/Client/**`, the Products/Categories/Dashboard DTOs and the
  `DashboardController`, and move the controller interfaces from `Client.Core` into Shared.
- Plan 016's .NET 10 migration is smaller after this plan lands.
