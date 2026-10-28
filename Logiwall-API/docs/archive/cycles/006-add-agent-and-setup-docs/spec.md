# Cycle 006 — A new developer or agent can set up, build, test and migrate the backend from README + CLAUDE.md alone: spec

## Contents

- L18 Summary
- L27 Why this matters
- L40 Repo facts you need (all verified at planning time; re-verify each by opening the cited file before you document it)
- L89 Scope
- L96 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 006 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2
- **Effort**: S
- **Risk**: LOW (docs only)
- **Depends on**: docs/archive/cycles/003-remove-committed-secrets-and-redact-logs/ (documents its config keys), docs/archive/cycles/004-patch-dotnet8-and-vulnerable-packages/ (SDK policy), docs/archive/cycles/005-establish-test-and-ci-baseline/ (test commands)
- **Category**: dx
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

`README.md` is one heading. Setting up the backend requires knowledge that is
written down nowhere:
- the EF command spans two projects;
- the SQL Server-only features rule out InMemory and SQLite;
- the required DB `AppSettings` rows are seeded only by migrations;
- MAUI breaks a whole-solution build;
- `ServiceResponse.Succuss` is misspelled but must not be renamed.

Agents executing the other plans in `docs/archive/audit-2026-10-27.md` keep running into these traps. A
README for humans and a CLAUDE.md for agents both pay for themselves right away.

## Repo facts you need (all verified at planning time; re-verify each by opening the cited file before you document it)

- The git root is `D:\Own Projects\Logiwall`. It contains `Logiwall-API/` (.NET
  backend, this repo) and `Logiwall-Front/` (React frontend, the real UI; it calls
  the `Identity`, `Person`, `Order` and `Common` controllers).
- Projects (`src/`):
  - `Logiwall.Domain`: EF entities. It references only `Microsoft.Extensions.Identity.Stores`.
  - `Logiwall.Application`: service interfaces only (`Abstractions/**`).
  - `Logiwall.Infrastructure`: EF `AppDbContext` (namespace is still
    `Logiwall.Server.Data`, a template leftover), migrations, services, external
    providers (SEP payment gateway `Providers/Sep724Provider.cs`, MyTipax courier
    `Providers/PostalServices/MyTipaxProvider.cs`, SMS `Services/Tools/SMSService.cs`),
    Mapperly mappers.
  - `Logiwall.Server`: ASP.NET Core host, controllers (`api/[controller]/[action]`),
    Identity and bearer auth, Swagger, Serilog file logs (`Logs/`).
  - `Logiwall.Shared`: DTOs, enums, resources, exceptions (shared with clients).
  - `Client/Logiwall.Client.Core`, `Client/Logiwall.Client.Web`, `Client/Logiwall.Client.Maui`:
    bit-bp template Blazor/MAUI clients. Server hosts Client.Web.
  - `Logiwall.Iac`: Pulumi Azure stack (App Service, Azure SQL, Key Vault, App Insights).
- Conventions:
  - Controllers are `partial` and get dependencies through `[AutoInject]` fields
    (the Bit.SourceGenerators source generator). Services use constructor injection
    and are registered in `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`.
  - Services return `ServiceResponse<T>` with `Data`, `Succuss` (misspelled; a wire contract; never rename) and `Message`.
  - Controllers turn `Succuss == false` into `BadRequest(result)`.
  - Global exception mapping: `src/Logiwall.Server/Services/ServerExceptionHandler.cs`, using the `KnownException` subclasses in `src/Logiwall.Shared/Exceptions`.
  - Every controller requires auth (`Program.Middlewares.cs`, `MapControllers().RequireAuthorization()`), except `[AllowAnonymous]` `IdentityController` and `CommonController`.
- Commands:
  - Build: `dotnet build Logiwall.Server.slnf` (created by plan 005). **Never** `dotnet build Logiwall.sln` (MAUI needs workloads).
  - Tests: `dotnet test tests/Logiwall.UnitTests`, and `dotnet test tests/Logiwall.IntegrationTests` (needs Docker; real SQL Server through Testcontainers).
  - EF (from `src/Logiwall.Server`): `dotnet tool restore`, then `dotnet ef migrations add <Name> --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` and `dotnet ef database update --project ../Logiwall.Infrastructure --startup-project .`.
  - Run: `dotnet run --project src/Logiwall.Server`. It listens on `http://localhost:5030` (`Properties/launchSettings.json`). Swagger is at `/swagger`.
- Configuration (after plan 003):
  - `ConnectionStrings:SqlServerConnectionString`
  - `Providers:Sep724:{Username,Password,Client,BasicAuthorization,Terminal}`
  - `Providers:MyTipax:{TokenUsername,TokenPassword,LoginUsername,LoginPassword}`
  - `Sms:PersiaFava:{Url,ApiKey,SenderNumber}`

  Set them with `dotnet user-secrets set "<key>" "<value>" --project src/Logiwall.Server`.
  Non-secret runtime settings (URLs, `LogiwallShare`, IBANs, `MyTipaxInquiryVersion`,
  `LoginOtpText`) are rows in the DB table `AppSettings`, seeded by migrations and
  read by `Infrastructure/Services/Global/AppSettingService.cs`.
- Dev certificate: `src/Logiwall.Server/IdentityCertificate.pfx` is the template's
  public test certificate. It is accepted only when `ASPNETCORE_ENVIRONMENT=Development`
  (`Program.Services.cs:170-174`). Production must supply its own certificate.
- SQL Server-only features: sequence `dbo.OrderSequence`, `HasDefaultValueSql` using
  `FORMAT`, and filtered unique indexes. EF InMemory and SQLite will not work.
- Plans live in `docs/archive/audit-2026-10-27.md`, indexed in this cycle's `tasks.md`.

## Scope

**In scope**: `README.md` (rewrite), `CLAUDE.md` (create), `AGENTS.md` (create, one line pointing to CLAUDE.md).

**Out of scope**: any code or config change; `src/Logiwall.Iac/Readme.md`
(mention it as a link only); docs for `Logiwall-Front`.

## Maintenance notes

- Update CLAUDE.md whenever a plan changes a convention: plan 013 (ILogger),
  plan 016 (net10), plan 015 (namespace rename).
- Keep secrets out: docs use placeholders only.
