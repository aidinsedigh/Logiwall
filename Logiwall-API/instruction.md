# Logiwall API Project Instructions

Where a section's rules live in `docs/conventions/`, the heading stays here and points to the file
that has the detail. Load that file only when the task actually touches it. See `docs/README.md`
for the full docs layout.

## 1. Project

Project name: **Logiwall API**

Logiwall API is the backend for Logiwall's parcel-shipping app: OTP login, customers and
contacts, orders, courier quotes (MyTipax) and payment (SEP 724). The real UI is the React app
in `../Logiwall-Front/`. It calls the `Identity`, `Person`, `Order` and `Common` controllers.

The stack (versions from the `*.csproj` files and `global.json`):

* .NET 8 (`net8.0`), C# 12, SDK pinned in `global.json` with `rollForward: disable`
* ASP.NET Core 8 MVC controllers
* Entity Framework Core 8 with Microsoft SQL Server
* ASP.NET Core Identity with bearer tokens
* Serilog (file sink, `src/Logiwall.Server/Logs/`)
* Riok.Mapperly for mapping
* Bit.SourceGenerators (`[AutoInject]`) and Bit.CodeAnalyzers
* Swashbuckle (Swagger)
* Pulumi for Azure infrastructure (`src/Logiwall.Iac/`)

The project was made from the bit Boilerplate (bit-bp) template. The `Client/*` projects are
template leftovers that the server still hosts.

### 1.1 Commands

| Purpose | Command | Notes |
|---|---|---|
| Build the server | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | Builds 7 projects. Verified 2026-10-27: 0 errors. |
| Build server and tests | `dotnet build Logiwall.Server.slnf` | Every project except MAUI and IaC. CI builds this. |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` | |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | Needs Docker (Testcontainers SQL Server) |
| Run | `dotnet run --project src/Logiwall.Server` | `http://localhost:5030`, Swagger at `/swagger` |
| EF tool | `cd src/Logiwall.Server && dotnet tool restore` | |
| Add a migration | `dotnet ef migrations add <Name> --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` | Run from `src/Logiwall.Server` |
| Update the database | `dotnet ef database update --project ../Logiwall.Infrastructure --startup-project .` | Run from `src/Logiwall.Server` |

Do not build `Logiwall.sln` or `Logiwall.Web.slnf`. Both contain `Logiwall.Client.Maui`, which
needs MAUI workloads. The tests and `Logiwall.Server.slnf` came with plan 005. CI is
`../.github/workflows/api-ci.yml` (at the git root).

---

# 2. Architecture

Logiwall API is **one deployable ASP.NET Core application with layered projects**.

```text
src/
├── Logiwall.Server/          host: controllers, auth, middleware, Swagger, Serilog
├── Logiwall.Application/     service interfaces only (Abstractions/<Area>/I*Service.cs)
├── Logiwall.Infrastructure/  AppDbContext, migrations, services, providers, mappers, DI
├── Logiwall.Domain/          EF entities (Entities/<Area>/)
├── Logiwall.Shared/          DTOs, enums, exceptions, resources, extensions (shared with clients)
├── Logiwall.Iac/             Pulumi Azure stack
└── Client/                   bit-bp Blazor/MAUI template clients (Core, Web, Maui)
```

### Reference rules

These are the project references that exist now (`*.csproj`). Keep them:

```text
Logiwall.Shared          → nothing in this solution
Logiwall.Domain          → Shared
Logiwall.Application     → Domain, Shared
Logiwall.Infrastructure  → Application, Domain, Shared
Logiwall.Server          → Application, Infrastructure, Shared, Client.Web
```

* Do not make `Domain`, `Application` or `Shared` reference `Infrastructure` or `Server`.
* Do not add business logic to `Logiwall.Server`. Controllers call a service and shape the HTTP result.
* `AppDbContext` is in `src/Logiwall.Infrastructure/Data/`, but its namespace is
  `Logiwall.Server.Data` (a template leftover). Do not rename it outside plan 015.
* Do not add features to `Client/*` unless the task asks for it. Plan 015 decides their future.

Do not migrate to a modular monolith, Minimal APIs or MediatR. A change of
architecture needs its own ADR and the user's decision (`docs/adr/0002-keep-layered-architecture.md`).

---

# 3. Where a Feature Lives

Code is grouped by **area** inside each project. The areas are `Global`, `Identity`,
`OrderItems`, `People`, plus `Providers` and `Tools` for external systems.

One endpoint touches these places:

```text
Logiwall.Server/Controllers/<Area>/<X>Controller.cs        HTTP endpoint ([AutoInject] the service)
Logiwall.Application/Abstractions/<Area>/I<X>Service.cs     the service contract
Logiwall.Infrastructure/Services/<Area>/<X>Service.cs       the implementation
Logiwall.Infrastructure/Services/DependencyInjection.cs     the DI registration
Logiwall.Shared/Dtos/<Area>/<Action>RequestDto.cs           request/response contracts
Logiwall.Domain/Entities/<Area>/<Entity>.cs                 entities
Logiwall.Infrastructure/Data/Configurations/<Area>/         EF configuration
Logiwall.Infrastructure/Mapper/<Area>Mapper.cs              Mapperly mappers
Client/Logiwall.Client.Core/Controllers/<Area>/I<X>Controller.cs   client-side contract the controller implements
```

The controller folders use `Order` where the other projects use `OrderItems`. Follow the folder
that already exists in the project you edit. Do not rename folders outside a plan.

Put a new file beside the files of the same area. Do not create a new top-level folder for one feature.

---

# 4. Services and the Request Path

The request path is:

```text
<X>Controller  ──calls──▶  I<X>Service (Application)  ──implemented by──▶  <X>Service (Infrastructure)
                                                                               │ reads/writes
                                                                               ▼
                                                                AppDbContext, providers (MyTipax, SEP, SMS)
```

* A service method returns `ServiceResponse<T>` (`src/Logiwall.Shared/Dtos/Global/ServiceResponse.cs`).
* The controller turns `Succuss == false` into `BadRequest(result)`, else `Ok(result)`.
* There is no CQRS and no MediatR. Do not add them for one feature.
* Do not add a repository, generic repository or unit-of-work over `AppDbContext`. Services use
  `AppDbContext` directly.

---

# 5. Domain Model and Existing Database

The entities map an existing SQL Server database. Before you create or change an entity:

1. Read the entity in `src/Logiwall.Domain/Entities/<Area>/`.
2. Read its configuration in `src/Logiwall.Infrastructure/Data/Configurations/<Area>/`.
3. Read the latest migration snapshot in `src/Logiwall.Infrastructure/Data/Migrations/`.
4. Check keys, foreign keys, indexes, constraints, nullability and sequences.
5. Understand the business meaning before you change the shape.

Never generate entities blindly from table names. A schema change always needs a migration.

---

# 6. Entity Framework Core and SQL Server

See `docs/conventions/ef-core.md`.

---

# 7. API Design, Contracts, Validation and Pagination

See `docs/conventions/api-design.md`.

---

# 8. Error Handling

See `docs/conventions/errors.md`.

---

# 9. Authentication, Authorization and Security

See `docs/conventions/auth-and-security.md`.

---

# 10. Logging and Observability

See `docs/conventions/logging-and-observability.md`.

---

# 11. Async, Cancellation and Dependency Injection

See `docs/conventions/async-and-di.md`.

---

# 12. Testing and QA Files

See `docs/conventions/testing-and-qa.md`.

---

# 13. .NET, Build and Deployment

See `docs/conventions/dotnet-and-deploy.md`.

---

# 14. Git and Code Quality

See `docs/conventions/git-and-code-quality.md`.

---

# 15. Shared Code

`Logiwall.Shared` is referenced by the server **and** by the clients. Everything in it ships to
every consumer.

Before you add something to `Logiwall.Shared`, ask:

> Does a client or more than one server project need this?

If not, keep it in the project that uses it (`Infrastructure` or `Server`).

* Put DTOs, enums, exceptions and resource strings in `Shared`.
* Do not put EF Core code, provider credentials or server-only logic in `Shared`.
* Before you write a one-line helper, check `src/Logiwall.Shared/Extensions/`. If a generic
  helper is missing, add it there with a doc comment. Do not declare a private copy in a service.

Do not make `Shared` a dumping ground.

---

# 16. Scope Control

When implementing a requested feature:

Do not silently:

* refactor unrelated code
* rename unrelated classes
* change unrelated APIs
* upgrade unrelated dependencies
* rewrite unrelated code

If you discover an unrelated issue:

1. mention it
2. do not silently fix it
3. continue the requested task unless it blocks implementation

The audit plans in `docs/work/001-audit-remediation/` already cover many known issues. If an
issue you find is in a plan, name the plan instead of fixing it.

---

# 17. Definition of Done

A change is complete only when the applicable items are satisfied:

* Requirements understood
* Existing code inspected
* Existing architecture understood
* `codebase-memory-mcp` used
* Layer reference rules respected (§2)
* Area folders respected (§3)
* Validation implemented
* Authorization and resource ownership implemented
* Error handling implemented
* Logging follows the Logiwall conventions
* CancellationToken propagated
* Tests implemented, when a test project exists (plan 005)
* QA file created/updated for a new or changed endpoint
* Acceptance criteria covered
* Security reviewed
* Performance considered
* `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` succeeds
* Relevant tests pass
* No unrelated changes introduced
* Git diff reviewed
* The plan's row in its cycle's `tasks.md` updated, when the change is a plan

---

# 18. Agent Behavior

Before implementing code:

1. Understand the task.
2. Inspect the existing codebase.
3. Use `codebase-memory-mcp` for code search.
4. For complicated tasks, plan the change before implementing it.
5. Prefer existing project conventions over inventing new conventions.
6. Implement the smallest correct solution.
7. Test the implementation.
8. Review the final changes against the requirements.

When you execute a plan from `docs/work/`, read the whole plan first, run its drift check and
obey its STOP conditions.

Do not start coding blindly.

---

# 19. Final Principle

Build Logiwall API as a simple, secure, production-quality layered application.

Prefer:

```text
Simple
Explicit
Consistent with the existing code
Testable
Observable
Secure
Performant
```

Avoid:

```text
Over-engineering
Unnecessary abstractions
Generic repositories
God services
God classes
Shared dumping grounds
Architecture changes without an ADR
Premature optimization
Unrelated refactoring
```

Use engineering judgment rather than following patterns mechanically.

---

# 20. Explaining Output to People

This section applies to **every** output a person reads: chat replies, final reports, `docs/`
prose, QA files, PR descriptions, commit bodies and explanatory comments. The detail and the
examples are in `docs/conventions/explaining-output.md`. Load that file before you write a
plan, ADR, QA file, PR description, diagram or HTML page.

Use the three levels. A higher level adds to the lower levels; it does not replace them:

1. **Writing — always.** Write in Simplified Technical English (ASD-STE100, about 80%):
   * Put the result or the answer first.
   * Write a maximum of 20 words in an instruction and 25 words in a description.
   * Use the active voice and simple tenses. Use the imperative for instructions.
   * Use one name for one thing: the name the code uses, in backticks.
   * Give a source for each fact (`file_path:line`, a command, a `§N`). State what you did not verify.
2. **Diagram — when the answer has a structure** (a flow across components, a sequence, a
   lifecycle, relationships, dependencies). Use plain text in the terminal and Mermaid in
   Markdown files. Read the code first, use real names, and label each arrow with a verb.
3. **HTML page — when the answer is too large or has too many parts for the terminal** (about
   two screens or more), when interaction helps, or when the person asks. Write one
   self-contained file outside the repository, publish it with the `Artifact` tool if the
   session has one, and give a short summary in the terminal too. Put no secrets on it.
