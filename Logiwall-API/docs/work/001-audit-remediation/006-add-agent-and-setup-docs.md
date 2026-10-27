# Plan 006: A new developer or agent can set up, build, test and migrate the backend from README + CLAUDE.md alone

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/README.md Logiwall-API/CLAUDE.md Logiwall-API/AGENTS.md`
> The README is expected to be the single line `# Logiwall-api`. If it is not, STOP.
>
> **Amended 2026-10-27**: `CLAUDE.md`, `AGENTS.md`, `instruction.md` and `docs/` were created
> outside this plan, together with the agent workflow rules. They exist now, so do
> not STOP for them. Step 2 now means: add the build, test and EF commands to `instruction.md`
> §1.1 and the security rules to `docs/conventions/auth-and-security.md`. Do not make a second
> `CLAUDE.md`. Step 3 is done.

## Status

- **Priority**: P2
- **Effort**: S
- **Risk**: LOW (docs only)
- **Depends on**: docs/work/001-audit-remediation/003-remove-committed-secrets-and-redact-logs.md (documents its config keys), docs/work/001-audit-remediation/004-patch-dotnet8-and-vulnerable-packages.md (SDK policy), docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md (test commands)
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

Agents executing the other plans in `docs/work/001-audit-remediation/` keep running into these traps. A
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
- Plans live in `docs/work/001-audit-remediation/`, indexed in `docs/work/001-audit-remediation/tasks.md`.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Check each documented command still works | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Check the EF tool restores | `cd src/Logiwall.Server && dotnet tool restore` | success |
| Check that each file path cited in the docs exists | `ls <path>` | exists |

## Scope

**In scope**: `README.md` (rewrite), `CLAUDE.md` (create), `AGENTS.md` (create, one line pointing to CLAUDE.md).

**Out of scope**: any code or config change; `src/Logiwall.Iac/Readme.md`
(mention it as a link only); docs for `Logiwall-Front`.

## Git workflow

- Branch: `advisor/006-agent-and-setup-docs`. Commit message: `Document setup, commands and conventions`. Do NOT push.

## Steps

### Step 1: Write README.md

Sections, in this order:
1. **What this is**: two sentences (backend API for Logiwall's parcel-shipping app: OTP login, orders, courier quotes, payment).
2. **Prerequisites**: .NET 8 SDK (8.0.301+; see `global.json`), Docker (for SQL Server and the integration tests), optionally SQL Server LocalDB.
3. **First run**: start SQL Server
   (`docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=<choose one> -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`),
   set the connection string with user-secrets, apply migrations, run, open Swagger.
   Use placeholders like `<choose one>`, never a real password.
4. **Configuration**: a table of every key from "Repo facts → Configuration" with
   columns *Key | Secret? | Where it's set | Used by*, followed by a second table of
   the DB `AppSettings` rows (key names only; copy them from the properties of
   `src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs` as they exist after plan 003).
5. **Build & test**: the commands from "Repo facts → Commands".
6. **Migrations**: the add and update commands, plus "migrations live in `src/Logiwall.Infrastructure/Data/Migrations`".
7. **Deploy**: one paragraph plus a link to `src/Logiwall.Iac/Readme.md`, and a note that nothing runs migrations automatically.
8. **Project layout**: the project list from "Repo facts".

### Step 2: Write CLAUDE.md

Keep it to 80 lines or fewer, in imperative voice, with these sections:
- **Build/test**: the exact commands, plus the warning never to build `Logiwall.sln`.
- **Architecture**: one line per project; note that `AppDbContext` is in Infrastructure despite its `Logiwall.Server.Data` namespace.
- **Conventions**: `[AutoInject]` partial controllers; constructor injection in services; `ServiceResponse<T>`/`Succuss` (never rename it); `KnownException` subclasses for errors; route shape; register new services in `DependencyInjection.cs`.
- **Security rules**: every query by ID must include an ownership predicate (`t.Customer.Person.UserId == userId`); never log header values or request/response bodies; never put credentials in code, migrations or appsettings; never reintroduce a default password.
- **Database**: SQL Server only; tests use Testcontainers; EF commands.
- **Plans**: work items are in `docs/work/001-audit-remediation/`; follow a plan's STOP conditions; update `docs/work/001-audit-remediation/tasks.md`.

### Step 3: AGENTS.md

One line: `See CLAUDE.md — it applies to all coding agents.`

**Verify (all steps)**:
- Every command in README and CLAUDE.md has been run once, or, for the Docker/DB ones, at least checked for syntax. List in your report which ones you actually ran.
- `grep -nE "Password=[^<;]|api_key=" README.md CLAUDE.md` → no output (no secret-looking values).
- Every file path you cite exists (`ls` each one).

## Test plan

Docs only, so there are no automated tests. The verification is that each
documented command runs as written.

## Done criteria

- [ ] `README.md` has the 8 sections, and `wc -l README.md` shows more than 60 lines
- [ ] `CLAUDE.md` exists with at most 80 lines, and `AGENTS.md` exists
- [ ] The secret-pattern grep returns nothing
- [ ] Only these 3 files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- A documented command fails, for example if plan 005's `Logiwall.Server.slnf`
  doesn't exist yet. Plan 005 is a dependency, so report instead of documenting
  something that doesn't work.
- `AppSettingsDto` still contains credential properties (`SepConnectPassword` and
  similar). That means plan 003 hasn't landed, and documenting those keys as DB
  rows would be wrong.

## Maintenance notes

- Update CLAUDE.md whenever a plan changes a convention: plan 013 (ILogger),
  plan 016 (net10), plan 015 (namespace rename).
- Keep secrets out: docs use placeholders only.
