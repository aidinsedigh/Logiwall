# Cycle 006 — plan

## Contents

- L32 Git workflow
- L36 STOP conditions
- L45 Commands you will need
- L53 Tasks
  - L55 Task 1: Write README.md
  - L73 Task 2: Write CLAUDE.md
  - L83 Task 3: AGENTS.md
- L92 Test plan
- L97 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/README.md Logiwall-API/CLAUDE.md Logiwall-API/AGENTS.md`
> The README is expected to be the single line `# Logiwall-api`. If it is not, STOP.
>
> **Amended 2026-10-27**: `CLAUDE.md`, `AGENTS.md`, `instruction.md` and `docs/` were created
> outside this plan, together with the agent workflow rules. They exist now, so do
> not STOP for them. Task 2 now means: add the build, test and EF commands to `instruction.md`
> §1.1 and the security rules to `docs/conventions/auth-and-security.md`. Do not make a second
> `CLAUDE.md`. Task 3 is done.

## Git workflow

- Branch: `advisor/006-agent-and-setup-docs`. Commit message: `Document setup, commands and conventions`. Do NOT push.

## STOP conditions

- A documented command fails, for example if plan 005's `Logiwall.Server.slnf`
  doesn't exist yet. Plan 005 is a dependency, so report instead of documenting
  something that doesn't work.
- `AppSettingsDto` still contains credential properties (`SepConnectPassword` and
  similar). That means plan 003 hasn't landed, and documenting those keys as DB
  rows would be wrong.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Check each documented command still works | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Check the EF tool restores | `cd src/Logiwall.Server && dotnet tool restore` | success |
| Check that each file path cited in the docs exists | `ls <path>` | exists |

## Tasks

### Task 1: Write README.md

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

### Task 2: Write CLAUDE.md

Keep it to 80 lines or fewer, in imperative voice, with these sections:
- **Build/test**: the exact commands, plus the warning never to build `Logiwall.sln`.
- **Architecture**: one line per project; note that `AppDbContext` is in Infrastructure despite its `Logiwall.Server.Data` namespace.
- **Conventions**: `[AutoInject]` partial controllers; constructor injection in services; `ServiceResponse<T>`/`Succuss` (never rename it); `KnownException` subclasses for errors; route shape; register new services in `DependencyInjection.cs`.
- **Security rules**: every query by ID must include an ownership predicate (`t.Customer.Person.UserId == userId`); never log header values or request/response bodies; never put credentials in code, migrations or appsettings; never reintroduce a default password.
- **Database**: SQL Server only; tests use Testcontainers; EF commands.
- **Plans**: work items are in `docs/archive/audit-2026-10-27.md`; follow a plan's STOP conditions; update this cycle's `tasks.md`.

### Task 3: AGENTS.md

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
- [ ] this cycle's `tasks.md` status row updated
