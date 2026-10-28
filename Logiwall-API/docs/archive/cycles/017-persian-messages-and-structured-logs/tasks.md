# Cycle 017 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/017-persian-messages-and-logs` (create it before Task 1, as
`plan.md` "Git workflow" says).

Scope, in one line each:

* Every client message is Persian (ADR-0003, written in Task 1).
* Every log entry has the structured format and a correlation ID (ADR-0004, written in Task 1).
* 19 open items from cycle 1 and the .NET 10 migration (`spec.md` §7).

## Tasks

- [x] Task 1 — ADR-0003, ADR-0004, conventions, `instruction.md` §17 (FR-D1-FR-D4)
- [x] Task 2 — `fa-IR` for every request; complete Persian resources; completeness test (FR-M2, FR-M4)
- [x] Task 3 — Client messages from resource keys; no provider text (FR-M1, FR-M3, FR-M5, FR-M9, FR-O13)
- [x] Task 4 — Persian binding, JSON, 429 and unknown-error messages (FR-M6-FR-M8)
- [x] Task 5 — Correlation ID middleware; Serilog from `appsettings.json` (FR-L2-FR-L4, FR-L6, FR-L8)
- [x] Task 6 — `ServerExceptionHandler` logs each handled exception once (FR-L5)
- [x] Task 7 — Refactor the 20 log calls to the template (FR-L1, FR-L7)
- [x] Task 8 — Account delete archives the user (FR-O1)
- [x] Task 9 — Atomic OTP attempts; SMS failure; login key; code range (FR-O2, FR-O4, FR-O5, FR-O14)
- [x] Task 10 — `OrderDto.Status`; parallel re-quote lock (FR-O3, FR-O15)
- [x] Task 11 — Used national code; all pending payments; SEP split test (FR-O6-FR-O8)
- [x] Task 12 — One token signer; new development certificate (FR-O9, FR-O10)
- [x] Task 13 — Dead code, `KnownIPNetworks`, remove health-check UI (FR-O11, FR-O12, FR-O16, FR-O17)
- [x] Task 14 — Client loading overlay and unresolved Bit tags (FR-O18, FR-O19)
- [x] Task 15 — Final check, self-review, archive the cycle

## Not in this cycle

* Plan 012 Phase B (SEP verify): stays BLOCKED in `docs/archive/cycles/012-verify-payments-server-side/tasks.md`.
* Operator actions and product decisions in cycle 1's `tasks.md`.
* IaC, the MAUI build, and the old client CSS variables (`spec.md` §2).

## Results

Closed on 2026-10-28, branch `advisor/017-persian-messages-and-logs` (not pushed).

### Final counts

| Check | Result |
|---|---|
| `dotnet build Logiwall.Server.slnf` | 0 errors |
| `dotnet test tests/Logiwall.UnitTests` | 20 passed (16 before the cycle) |
| `dotnet test tests/Logiwall.IntegrationTests` | 60 passed, 1 skipped (32 + 1 before) |
| Vulnerability audit, `Logiwall.Server` and `Logiwall.Client.Web` (nuget.org config) | No vulnerable package |
| Build warnings `ASPDEPR005`, `RZ10012`, `RMG020` for `Order.State` | None |
| Live server, `Accept-Language: en-US`, invalid body | 400; both messages Persian; `Request-Id` = `X-Correlation-Id`; both log lines carry the ID |
| Browser, `/sign-in` | Overlay in the first HTML, removed when interactive; no console error |

### Self-review: where the work differs from the plan

* Task 2: the test reads the `fa` resource set, not `fa-IR`. The satellite assembly is `fa`, and
  `tryParents: false` finds nothing for `fa-IR`. The test also rejects ASCII letters (FR-M1), so
  12 more values changed: 9 `ToDo` placeholders, `OtpCodeIsNotCorrect` and `OtpCodeIsExpired`
  ("otp") and `LimitedPackageSize` ("x"). 81 `AppStrings` keys were missing, not 74.
* Task 3: `Sep724Provider.cs:42` (`"Invalid Client"`) still matches the verify grep. Spec §4.4
  keeps it: it never reaches a client, and `OrderService` now logs it as `{ProviderMessage}`.
* Task 4: attributes without a resource key (`[Required, StringLength(7)]`, `[RegularExpression]`)
  produce English text. `ToPersianError` (`Program.Services.cs`) replaces any model error that
  has an ASCII letter with `ModelBindingValueIsInvalid`.
* Task 5: the `Sanitise` tests are in the integration project. Only that project references
  `Logiwall.Server`.
* Task 6: the test sink is a Serilog `ILogEventSink`, not an `ILoggerProvider`. `UseSerilog`
  ignores other providers; `ReadFrom.Services` picks up the sink. A5 held: 0 entries before the
  change, 1 after, so no `SuppressDiagnosticsCallback`.
* Task 7: `LogUserLoginAsync` declares `user` before `try`, so the `catch` can log `{UserId}`.
  `Program.cs` logs "Web host is starting." (it runs before the host starts).
* Task 9 (FR-O4): removing `timestamp` also makes the stored code read as expired. A customer
  whose SMS failed requests a new code, now without a cooldown.
* Task 11 (FR-O7): order of checks is verified `Success`, then each pending payment (newest
  first), then a stored `UnSuccess`. The spec's "stored result first" would return an old failure
  without checking a newer pending retry.
* Task 12 (FR-O10): the new certificate is a self-signed RSA 2048 certificate made in memory
  (CN=Logiwall Development Identity, valid to 2031-10-28). `dotnet dev-certs https --export-path`
  would export this machine's trusted localhost HTTPS key. The integration tests' own certificate
  helper is deleted; the tests now sign with the committed certificate.
* Extra fix (found in the live check): `ServerExceptionHandler` logged `{Path}` as `/`, because
  `UseExceptionHandler("/")` changes `Request.Path`. It now reads `IExceptionHandlerFeature.Path`.

### Not verified

* FR-O10 manual check with `dotnet run` and an OTP login: the local server has no SMS
  configuration, so no code arrives. The integration tests cover the same path with the committed
  certificate (`Otp_token_and_password_token_both_authorize`).
* That the FR-O2 parallel test fails on the old code. It passed 3 times on the new code.
* A dev database whose data-protection keys were encrypted with the old certificate: those keys
  cannot be decrypted with the new one.
* The MAUI client (no workload on this machine).

### Seen, not fixed (out of scope)

* `ButtonStyle="BitButtonStyle.…"` in 11 client files: Bit 10 has no `ButtonStyle`
  parameter, so it renders as a plain HTML attribute (no build warning).
* Mapperly `RMG020`: `Order.OrderNumber` has no `OrderDto` property.
* `src/Client/Logiwall.Client.Web/wwwroot/service-worker.js:28` still excludes `/healthchecks-ui/`.
