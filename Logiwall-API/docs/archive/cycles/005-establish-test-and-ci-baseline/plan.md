# Cycle 005 — plan

## Contents

- L38 Git workflow
- L44 STOP conditions
- L57 Commands you will need
- L67 Tasks
  - L69 Task 1: Make the host testable (two production one-liners)
  - L80 Task 2: Create the unit test project
  - L103 Task 3: Create the integration test project and harness
  - L147 Task 4: Smoke test
  - L156 Task 5: Regression tests for plans 001 and 002
  - L187 Task 6: Server-only solution filter and CI
- L221 Test plan
- L227 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Server/Program.cs Logiwall-API/src/Logiwall.Server/Program.Services.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs Logiwall-API/Logiwall.sln`
> Plans 001–004 are prerequisites, so their changes are expected. `Program.Services.cs`
> will show plan 003's `AddInfrastructure(configuration)` change. If the
> specific lines quoted below differ, STOP.
>
> **Amended 2026-10-27 — Central Package Management**: package versions now live in
> `Logiwall-API/Directory.Packages.props`. It also applies to the new `tests/` projects. Add
> each test package with `dotnet add package`, which writes the version to
> `Directory.Packages.props`, or add a `PackageVersion` there by hand. Never put a `Version`
> on a `PackageReference`. If the CI workflow filters by path, include
> `Directory.Packages.props`.

## Git workflow

- Branch: `advisor/005-test-and-ci-baseline`
- Commits: one per step, e.g. `Add integration test harness on SQL Server Testcontainers`.
- Do NOT push.

## STOP conditions

- `docker info` fails. Finish Tasks 1–3 and 6, confirm that the integration
  project **builds**, and stop with the report "integration tests not executed
  locally: no Docker".
- The factory fails to start with an error about the certificate, `HostAbortedException`
  or the connection string, even after Task 1. Report the full exception;
  don't change other production code.
- `MigrateAsync` fails on a migration. That means the migration chain is broken
  on a fresh database, which is a real finding. Report the migration name.
- A regression test from Task 5 fails. Plan 001 or 002 is incomplete. Report it,
  and don't fix the production code here.

## Commands you will need

| Purpose | Command (from `Logiwall-API/`) | Expected on success |
|---|---|---|
| Docker available? | `docker info --format "{{.ServerVersion}}"` | prints a version |
| Build server | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0 |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` | "Passed!" with 0 failed |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | "Passed!" with 0 failed (requires Docker) |
| Package version lookup | `curl -s https://api.nuget.org/v3-flatcontainer/<lowercase-id>/index.json` | JSON list of versions |

## Tasks

### Task 1: Make the host testable (two production one-liners)

1. `Program.cs`: change `catch (Exception exc)` to
   `catch (Exception exc) when (exc is not HostAbortedException)`.
   `HostAbortedException` is in `Microsoft.Extensions.Hosting`; add that `using` if needed.
2. `Program.Services.cs:167` and `IdentityController.cs:306`: replace
   `Directory.GetCurrentDirectory()` with `AppContext.BaseDirectory`.

**Verify**: `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` → exit 0, and
`grep -rn 'GetCurrentDirectory(), "IdentityCertificate.pfx"' src` → no output.

### Task 2: Create the unit test project

```
dotnet new xunit -o tests/Logiwall.UnitTests --framework net8.0
dotnet add tests/Logiwall.UnitTests reference src/Logiwall.Shared/Logiwall.Shared.csproj src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj
dotnet sln Logiwall.sln add tests/Logiwall.UnitTests/Logiwall.UnitTests.csproj
```
Delete the generated `UnitTest1.cs`. Add `tests/Logiwall.UnitTests/Extensions/QueryableExtensionsTests.cs`
with characterization tests for `Logiwall.Shared.Extensions.QueryableExtensions`.
Run them against an in-memory `List<T>.AsQueryable()` of a small private record
type `Item(int Id, string Name)`:
- `ApplyPagination(pageNumber: 2, pageSize: 2)` over Ids 1..5 returns Ids 3,4.
- `ApplyFilters` with one group, one `Equal` condition on `Id` = 3, returns exactly Id 3.
- `ApplySorting` with `Ascending = false` on `Id` returns 5,4,3,2,1.
- `ApplyQueryParams` with `HasPagination = false` returns all 5 items.

The types live in `Logiwall.Shared.Dtos.Global` (`GetListQueryParams`,
`FilterGroup`, `FilterCondition`, `SortCondition`) and
`Logiwall.Shared.Enums.Constants` (`ConditionOperator`, `LogicalOperator`).
Open those files to confirm property names before writing the tests.

**Verify**: `dotnet test tests/Logiwall.UnitTests` → `Passed!  - Failed: 0, Passed: 4`.

### Task 3: Create the integration test project and harness

```
dotnet new xunit -o tests/Logiwall.IntegrationTests --framework net8.0
dotnet add tests/Logiwall.IntegrationTests reference src/Logiwall.Server/Logiwall.Server.csproj
dotnet add tests/Logiwall.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing --version <same 8.0.N as the server's ASP.NET packages>
dotnet add tests/Logiwall.IntegrationTests package Testcontainers.MsSql --version <latest stable 3.x or 4.x>
dotnet sln Logiwall.sln add tests/Logiwall.IntegrationTests/Logiwall.IntegrationTests.csproj
```
Create these files under `tests/Logiwall.IntegrationTests/Infrastructure/`:

1. **`SqlServerFixture.cs`**: implements `IAsyncLifetime`. It starts an `MsSqlContainer`
   (`new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build()`),
   and exposes `ConnectionString`. It also exposes a `LogiwallApiFactory Factory`,
   created after the container starts. After the factory is created, the fixture
   resolves `AppDbContext` from `Factory.Services.CreateScope()` and calls
   `await db.Database.MigrateAsync()` once.
2. **`LogiwallApiFactory.cs`**: `class LogiwallApiFactory(string connectionString) : WebApplicationFactory<Logiwall.Server.AppSettings>`.
   In `ConfigureWebHost`:
   - `builder.UseEnvironment("Development");`
   - `builder.UseSetting("ConnectionStrings:SqlServerConnectionString", connectionString);`
   - Also set the process environment variable
     `ConnectionStrings__SqlServerConnectionString` to the same value **before**
     the factory is first used. `Program.Services.cs` reads the connection string
     eagerly while building the host, and the environment variable makes sure the
     override is visible at that point.
   - `builder.ConfigureTestServices(s => { s.RemoveAll<ISMSService>(); s.AddSingleton<FakeSmsService>(); s.AddSingleton<ISMSService>(sp => sp.GetRequiredService<FakeSmsService>()); });`
3. **`FakeSmsService.cs`**: implements `ISMSService`. It keeps a
   `ConcurrentDictionary<string, string>` holding the last message per destination,
   returns `"OK"`, and has a helper `string LastCodeFor(string phone)` that applies
   the regex `\d{5,6}` to the stored message.
4. **`ApiClient.cs`**: helpers on top of `HttpClient`:
   - `Task<string> LoginWithOtpAsync(string phone)`: SendOtpCode, then read the code
     from the fake, then ConfirmationOtpCode. Returns `data.token`. Request bodies:
     `{ phoneNumber, channel: "pwa", loginProvider: 20 }` and
     `{ phoneNumber, code, channel: "pwa", loginProvider: 20 }`.
   - `HttpClient Authed(string token)`: a client whose `Authorization` header is `Bearer <token>`.
   - `Task RegisterAsync(HttpClient authed, string password)`: `POST api/Person/Register` with
     `{ name: "Test", lastName: "User", nationalCode: "0000000000", password, confirmPassword: password }`.
   - `static string NewPhone()`: returns `"09" + 9 random digits`, so tests never collide.
5. **`IntegrationCollection.cs`**: `[CollectionDefinition("api")] public class IntegrationCollection : ICollectionFixture<SqlServerFixture> {}`.

**Verify**: `dotnet build tests/Logiwall.IntegrationTests` → exit 0.

### Task 4: Smoke test

`tests/Logiwall.IntegrationTests/Identity/OtpLoginTests.cs`, `[Collection("api")]`:
- `Otp_login_returns_a_bearer_token_that_authenticates`: log in with the OTP
  helper, then call `GET api/Person/GetUserProfile` with the token. Assert the
  status is **not** 401. It may be 400 "User is not registered"; that's fine.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → passes (Docker required; see STOP conditions).

### Task 5: Regression tests for plans 001 and 002

`tests/Logiwall.IntegrationTests/Identity/PasswordLoginTests.cs`:
1. `Otp_created_user_has_no_password_hash`: after the OTP login, query `Users`
   through `AppDbContext` for that phone and assert `PasswordHash` is null.
2. `Otp_created_user_cannot_get_token_with_any_password`: `POST api/Identity/GetToken` with
   `{ userName: phone, password: "Wrong-Pass-9", channel: "pwa", loginProvider: 20 }` → status 401.
   **Do not use the old shared default password literal anywhere.**
3. `Registered_user_can_get_token_with_their_password`: OTP login, then Register with
   `"Strong-Pass-1"`, then GetToken with that password → 200, and the body contains `accessToken`.

`tests/Logiwall.IntegrationTests/People/CustomerContactOwnershipTests.cs`:
4. User A creates a contact (`POST api/Person/CreateCustomerContact`; read the
   required fields from `src/Logiwall.Shared/Dtos/People/CreateCustomerContactRequestDto.cs`)
   and lists contacts to get its `id`. User B calls `GET api/Person/GetCustomerContact?id=<id>` → 400.
5. User B calls `POST api/Person/UpdateCustomerContact` with that id and a different
   `address` → 400. Reload the row through `AppDbContext`: the address is unchanged.

`tests/Logiwall.IntegrationTests/Orders/OrderOwnershipTests.cs`:
6. Insert an `Order` for user A directly through `AppDbContext` (set every
   `[Required]` property of `src/Logiwall.Domain/Entities/OrderItems/Order.cs`
   and `CustomerId` = A's customer id; leave `OrderNumber` null, because SQL fills
   it). User B calls `GET api/Order/GetOptions?orderId=<id>` → 400 with message "Invalid OrderId".
7. Insert an `OrderInqueryOption` for a *second* order of user A, then call
   `POST api/Order/SelectOption` with `{ orderId: <first order>, orderInqueryOptionId: <option of second>, channel: "pwa", callbackUrl: "https://example.test/cb" }`
   → 400 with message "Invalid OrderInqueryOptionId". It must not reach the payment gateway.
   `OrderInqueryOption.ServiceProviderId` needs an existing `ServiceProviders`
   row; read one from the DB (the migrations seed them).

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all 8 tests pass.

### Task 6: Server-only solution filter and CI

1. Create `Logiwall.Server.slnf` listing every project in `Logiwall.sln` **except**
   `src\Client\Logiwall.Client.Maui\Logiwall.Client.Maui.csproj` and
   `src\Logiwall.Iac\Logiwall.Iac.csproj`, and including both test projects. Copy
   the JSON shape of the existing `Logiwall.Web.slnf`.
2. Create `D:\Own Projects\Logiwall\.github\workflows\api-ci.yml`:
   ```yaml
   name: api-ci
   on:
     push:
       paths: ['Logiwall-API/**', '.github/workflows/api-ci.yml']
     pull_request:
       paths: ['Logiwall-API/**', '.github/workflows/api-ci.yml']
   jobs:
     build-test:
       runs-on: ubuntu-latest
       defaults:
         run:
           working-directory: Logiwall-API
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with:
             global-json-file: Logiwall-API/global.json
         - run: dotnet build Logiwall.Server.slnf -c Release
         - run: dotnet test tests/Logiwall.UnitTests -c Release --no-build
         - run: dotnet test tests/Logiwall.IntegrationTests -c Release --no-build
   ```

**Verify**:
- `dotnet build Logiwall.Server.slnf` → exit 0 (it must not try to build MAUI).
- The YAML parses: `python -c "import yaml,sys; yaml.safe_load(open(r'D:\Own Projects\Logiwall\.github\workflows\api-ci.yml'))"` → no error. If Python is unavailable, say so.

## Test plan

This plan *is* the test plan. In total it adds 4 unit tests and 8 integration
tests. Later plans add tests here and follow the structure of
`Orders/OrderOwnershipTests.cs`.

## Done criteria

- [ ] `dotnet build Logiwall.Server.slnf` exits 0
- [ ] `dotnet test tests/Logiwall.UnitTests` → 4 passed, 0 failed
- [ ] `dotnet test tests/Logiwall.IntegrationTests` → 8 passed, 0 failed (or, without Docker locally: the project builds, and your report says the run is deferred to CI)
- [ ] `D:\Own Projects\Logiwall\.github\workflows\api-ci.yml` exists
- [ ] No production file changed except the three one-liners in Task 1 (`git status`)
- [ ] No test or fixture contains the old shared default password
- [ ] this cycle's `tasks.md` status row updated
