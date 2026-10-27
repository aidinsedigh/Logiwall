# Cycle 017 — plan

> **Executor instructions**: Read `tasks.md`, then this whole file, then `spec.md` sections as each
> task names them. Do the tasks in order. Run each verification and confirm the expected result
> before the next task. If a STOP condition occurs, stop and report; do not improvise. After each
> task, commit, then tick its line in `tasks.md`.

## Git workflow

* Branch from the current work branch: `git switch -c advisor/017-persian-messages-and-logs`.
* Commit once per task. Use Conventional Commits and end each message with the attribution line
  that the session gives. Do not push.

## Drift check (run first, from `Logiwall-API/`)

1. `git log --oneline -1` shows commit `8cec685` or a later commit on the same line.
2. `git grep -n "DeleteAsync(user)" -- src/Logiwall.Server/Controllers/Identity/UserController.cs`
   → one match.
3. `git grep -c "Message = \"" -- src/Logiwall.Infrastructure src/Logiwall.Server` → more than 0.
4. `ls docs/adr` → `0001-*` and `0002-*` only.

If check 2 or 3 gives no match, another change has done part of this cycle. STOP and report.

## STOP conditions

* A test that passed before the task fails, and the cause is not the task's own change.
* A fact in `spec.md` §9 (assumptions) is wrong.
* A task needs a database migration (NFR-5) or a new NuGet package (NFR-4).
* `Logiwall-Front` compares an English message text (Task 3 step 1 finds it).

## Commands

| Purpose | Command |
|---|---|
| Build | `dotnet build Logiwall.Server.slnf` |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` (needs Docker) |
| Vulnerability audit | `dotnet list src/Logiwall.Server/Logiwall.Server.csproj package --vulnerable --include-transitive` |

If the corporate NuGet feed returns HTTP 504 for the audit, add `--config <file>` with a
`nuget.config` that lists only `https://api.nuget.org/v3/index.json`. Keep that file outside the
repository.

---

### Task 1: Write the two decisions (FR-D1-FR-D4)

Files: `docs/adr/0003-persian-client-messages.md` (new), `docs/adr/0004-structured-log-format.md`
(new), `docs/conventions/errors.md`, `docs/conventions/logging-and-observability.md`,
`instruction.md`, `docs/README.md`, `Logiwall.sln`.

1. Write the two ADRs from `spec.md` §8.1 and §8.2. Use the shape of
   `docs/adr/0002-keep-layered-architecture.md`: Status, Context, Decision, Consequences.
2. Change the two convention files and `instruction.md` §17 as `spec.md` §8.3 says.
3. Add both ADRs to the ADR table in `docs/README.md`.
4. Add both ADR files to the `adr` solution folder in `Logiwall.sln`.

Verify: `git grep -n -i "shoplink\|adopted from" -- docs instruction.md` → no output.
Commit: `docs(adr): record persian client messages and the structured log format`.

### Task 2: Persian culture and complete Persian resources (FR-M2, FR-M4)

Files: `src/Logiwall.Server/Program.Middlewares.cs`, `src/Logiwall.Shared/Resources/AppStrings.fa.resx`,
`IdentityStrings.fa.resx`, `tests/Logiwall.UnitTests/Resources/PersianResourceTests.cs` (new).

1. Replace the `MultilingualEnabled` block in `Program.Middlewares.cs:28-37` with the call in
   `spec.md` §4.2.
2. Write the unit test first. For `AppStrings.ResourceManager` and `IdentityStrings.ResourceManager`,
   read the neutral set with `GetResourceSet(CultureInfo.InvariantCulture, true, false)` and the
   Persian set with `GetResourceSet(new CultureInfo("fa-IR"), true, false)`. Assert that each
   neutral key is in the Persian set and its value is not empty. Run it: it fails with 74 + 19
   missing keys.
3. Translate each missing key into Persian in the two `.fa.resx` files. Keep `{0}` placeholders.
4. Add the helper `tests/Logiwall.IntegrationTests/Infrastructure/PersianAssert.cs`
   (`spec.md` §4.5).
5. Integration test (`tests/Logiwall.IntegrationTests/Hardening/PersianCultureTests.cs`, new):
   call `api/Identity/GetToken` with a wrong password and the header `Accept-Language: en-US`.
   That path already uses a resource key (`IdentityController.cs:82`). Assert that the
   `message` of the body passes `PersianAssert.IsPersian`.

Verify: build; both test projects pass.
Commit: `feat(i18n): answer every request in persian and complete the persian resources`.

### Task 3: Client messages from resource keys (FR-M1, FR-M3, FR-M5, FR-M9)

Files: every site in `spec.md` §4.4; `AppStrings.resx`, `AppStrings.fa.resx`.

1. Search `../Logiwall-Front/src` for each English text in `spec.md` §4.4. If a file compares
   one of them, STOP (assumption A2).
2. Add the new keys of §4.4 to both `.resx` files. Regenerate nothing by hand:
   `AppStrings.Designer.cs` updates on build (`PublicResXFileCodeGenerator`). If it does not,
   add each property to `AppStrings.Designer.cs` in the same form as its neighbours.
3. At each site, inject `IStringLocalizer<AppStrings>` through the constructor where the class
   does not have it, and use `localizer[nameof(AppStrings.Key)]`.
4. `OrderService.cs:386`: log the provider `Title` with the template in `spec.md` §5.2 ("new
   entries"), then return `PaymentLinkFailed`. `:423`: return `PaymentLinkCreated`.
5. `IdentityController.cs:41-44`: apply the FR-O13 change (`spec.md` §7). Add the null check in
   `AuthenticationService.ConfirmationOtpCodeAsync`.
6. Integration tests: add `PersianAssert.IsPersian` to each existing test that asserts a failure
   with a message. Add one test for FR-O13.

Verify: `git grep -n -E 'Message\s*=\s*"[A-Za-z]' -- src/Logiwall.Infrastructure src/Logiwall.Server`
→ no output. Build; tests pass.
Commit: `feat(i18n): return persian resource messages from services and controllers`.

### Task 4: Persian framework errors (FR-M6, FR-M7, FR-M8)

Files: `src/Logiwall.Server/Program.Services.cs`, `src/Logiwall.Server/Services/ServerExceptionHandler.cs`.

1. Apply `spec.md` §4.3 (binding accessors, `AllowInputFormatterExceptionMessages = false`,
   `InvalidModelStateResponseFactory`).
2. `ServerExceptionHandler`: remove the Development branch at line 24, so an unknown exception
   always gives `localizer[nameof(UnknownException)]`.
3. Rate limiter: add `options.OnRejected` that writes a `RestErrorInfo` with
   `Key = nameof(TooManyRequestsExceptions)`, the Persian message, and
   `ExceptionType = typeof(TooManyRequestsExceptions).FullName`. Use the same
   `JsonSerializerOptions` as `ServerExceptionHandler`.
4. Integration tests: (a) a malformed JSON body to an `[ApiController]` action → 400, Persian;
   (b) a missing required field → Persian; (c) the rate limit test that exists (plan 007) →
   429 body has a Persian `message`. Use a factory with `relaxRateLimits: false`, as plan 007's
   test does.

Verify: build; tests pass.
Commit: `feat(i18n): return persian messages for binding, rate limit and unknown errors`.

### Task 5: Correlation ID and Serilog configuration (FR-L2, FR-L3, FR-L4, FR-L6, FR-L8)

Files: `src/Logiwall.Server/Services/CorrelationIdMiddleware.cs` (new), `Program.cs`,
`Program.Middlewares.cs`, `appsettings.json`, `ServerExceptionHandler.cs`.

1. Write the middleware and its registration as `spec.md` §5.3 says.
2. Move the sink configuration to `appsettings.json` and change `Program.cs` as §5.3 says.
3. Set the request-logging template (FR-L6).
4. Unit test for `CorrelationIdMiddleware.Sanitise` (make it `public static`, as a pure
   function): a valid value is kept; a value of 65 characters, an empty value and a value
   with a space each give a new GUID.
5. Integration test: a request with `X-Correlation-Id: test-123` gets the same header back; a
   request without it gets a GUID; a request that fails (for example an unknown order ID that
   throws) returns `Request-Id` equal to `X-Correlation-Id`.

Verify: build; tests pass; run the server and read one line of `src/Logiwall.Server/Logs/` →
the line has the format of §5.3 with the correlation ID.
Commit: `feat(logging): add a correlation id and read serilog from configuration`.

### Task 6: Log each handled exception once (FR-L5)

Files: `ServerExceptionHandler.cs`, a test log sink in `tests/Logiwall.IntegrationTests/Infrastructure/`.

1. Add a capturing `ILoggerProvider` to `LogiwallApiFactory` (an in-memory list of level,
   category, message template and exception). Do not add a package.
2. Write the test first: replace one service with `ConfigureTestServices` so it throws
   `InvalidOperationException`, call its endpoint, and count `Error` entries with that exception.
   Record the count before your change (assumption A5).
3. Add the two log calls of `spec.md` §5.2 to `ServerExceptionHandler` (inject
   `ILogger<ServerExceptionHandler>`). If step 2 counted one entry already, also set
   `SuppressDiagnosticsCallback = _ => true` on `UseExceptionHandler`.
4. The test asserts exactly one `Error` entry for an unknown exception, and one `Warning` entry
   for a `ResourceNotFoundException`.

Verify: build; tests pass.
Commit: `fix(logging): log each handled exception once in the structured format`.

### Task 7: Refactor every log call (FR-L1, FR-L7)

Files: the 20 sites in `spec.md` §5.2.

1. Change each call to the template in the table. Keep the arguments; add only the fields the
   table names, under the rule below the table.
2. `Program.cs`: keep the bootstrap logger for these two calls.

Verify:
* `git grep -n -E '\.Log(Information|Warning|Error|Debug|Critical|Trace)\(' -- src/Logiwall.Server src/Logiwall.Infrastructure`
  → every match has a template that starts with `"[` on the same or the next line.
* `git grep -n -E 'Log\w*\([^)]*\$"' -- src` → no output (no interpolated template).
* Build; tests pass.
Commit: `refactor(logging): use the structured template in every log call`.

### Task 8: Account delete archives the user (FR-O1)

Files: `src/Logiwall.Server/Controllers/Identity/UserController.cs`.

1. Apply `spec.md` §7 FR-O1.
2. Integration test (`tests/Logiwall.IntegrationTests/Identity/AccountDeleteTests.cs`, new):
   register a user with an order and a pending payment (`OrderTestData`); call `DELETE api/User/Delete`;
   assert the rows exist, `IsArchive` is true, and `SendOtp` returns the Persian `AccountIsDisabled`.

Verify: build; tests pass.
Commit: `fix(identity): archive the account on delete instead of deleting its records`.

### Task 9: OTP fixes (FR-O2, FR-O4, FR-O5, FR-O14)

Files: `src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs`.

1. FR-O2: the atomic counter of `spec.md` §7. The `UserTokens` key is (`UserId`, `LoginProvider`,
   `Name`) (`AppDbContextModelSnapshot.cs:964-966`).
2. FR-O4, FR-O5, FR-O14 as `spec.md` §7 says.
3. Tests of `spec.md` §7 for FR-O2, FR-O4 and FR-O5 in `tests/Logiwall.IntegrationTests/Identity/`.
   Make `FakeSmsService` fail on demand if it cannot yet.

Verify: build; tests pass. Run the FR-O2 test 3 times; it passes each time.
Commit: `fix(identity): make the otp attempt cap atomic and keep login records unique`.

### Task 10: Order fixes (FR-O3, FR-O15)

Files: `src/Logiwall.Infrastructure/Mapper/OrderMapper.cs`, `OrderService.cs`.

1. FR-O3 and FR-O15 as `spec.md` §7 says.
2. Tests of `spec.md` §7 in `tests/Logiwall.IntegrationTests/Orders/`.

Verify: build output has no `RMG020` line that names `State`; tests pass.
Commit: `fix(order): show the order state and serialize parallel re-quotes`.

### Task 11: People and payment fixes (FR-O6, FR-O7, FR-O8)

Files: `PersonService.cs`, `PaymentService.cs`, `tests/Logiwall.UnitTests/Providers/`.

1. FR-O6 and FR-O7 as `spec.md` §7 says, with their integration tests.
2. FR-O8: move the private `FakeHttpProvider` out of `ProviderTokenCachingTests.cs` into its own
   `internal` file when the new test needs it, then write the split test.

Verify: build; tests pass.
Commit twice, one per area: `fix(people): reject a national code that is in use` and
`fix(payment): verify every pending payment of the order`. Put the FR-O8 test in the payment commit.

### Task 12: One token signer and a valid dev certificate (FR-O9, FR-O10)

Files: `IdentityController.cs`, `src/Logiwall.Server/IdentityCertificate.pfx`, `Program.Services.cs:191`.

1. FR-O9 as `spec.md` §7 says. Remove the `using` lines that only `GenerateJwtToken` needed.
2. FR-O10: make the certificate and update the thumbprint. Do not print or commit any password.
3. Test of `spec.md` §7 FR-O9.

Verify: build; tests pass; manual check of FR-O10.
Commit: `refactor(identity): sign otp tokens with the bearer token protector` and
`chore(identity): replace the expired development certificate`.

### Task 13: Dead code and package hygiene (FR-O11, FR-O12, FR-O16, FR-O17)

1. Grep each symbol before you delete it. Then apply `spec.md` §7.
2. Run the vulnerability audit for `Logiwall.Server` and `Logiwall.Client.Web`.

Verify: build has no `ASPDEPR005`; audit shows no advisory; tests pass.
Commit: `refactor(server): remove dead code and the unused health check ui`.

### Task 14: Client defects (FR-O18, FR-O19)

Files: `src/Logiwall.Server/Components/App.razor`, `src/Client/Logiwall.Client.Core/Scripts/app.ts`,
`MainLayout.razor.cs`, and the 17 `.razor` files that `spec.md` §7 FR-O19 lists.

1. Apply `spec.md` §7.
2. Manual check: start SQL Server in Docker, apply the migrations, run the server, open
   `http://localhost:5030/sign-in` in a browser. The loading overlay goes away when the page is
   interactive, and the console has no error.

Verify: build output has no `RZ10012` for the three names; tests pass.
Commit: `fix(client): remove the loading overlay and replace unresolved bit components`.

### Task 15: Final check and close the cycle

1. Run the build, both test projects and the audit. Record the counts in `tasks.md`.
2. Run the server; send one request with `Accept-Language: en-US` that fails; confirm the body
   is Persian and the log line has the correlation ID.
3. Write a short self-review at the end of `tasks.md`: what changed, what was not verified.
4. Move `docs/work/017-persian-messages-and-structured-logs/` to `docs/archive/cycles/` with the
   TOC that `docs/README.md` describes, and move its solution folder in `Logiwall.sln`. Update
   the "Current Work" table in `docs/README.md`.

Commit: `docs(work): close cycle 017`.
