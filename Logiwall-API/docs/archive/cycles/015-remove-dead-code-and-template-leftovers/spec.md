# Cycle 015 — Unused packages, dead code and misleading template namespaces are gone, so the code shows only what actually runs: spec

## Contents

- L18 Summary
- L27 Why this matters
- L39 Repo facts you need
- L59 Scope
- L75 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 015 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P3
- **Effort**: S–M (mechanical; compiler-verified)
- **Risk**: LOW
- **Depends on**: docs/archive/cycles/005-establish-test-and-ci-baseline/, docs/archive/cycles/008-fix-otp-send-path-and-async-sms/ (removes the last Newtonsoft use), docs/archive/cycles/012-verify-payments-server-side/ (makes `PaymentRequestDto` unused). Run it after all of plans 001–014.
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

## Maintenance notes

- Decision pending (product): keep the Blazor/MAUI clients and template dashboard, or go API-only.
  If API-only, a follow-up plan should remove `src/Client/**`, the Products/Categories/Dashboard DTOs and the
  `DashboardController`, and move the controller interfaces from `Client.Core` into Shared.
- Plan 016's .NET 10 migration is smaller after this plan lands.
