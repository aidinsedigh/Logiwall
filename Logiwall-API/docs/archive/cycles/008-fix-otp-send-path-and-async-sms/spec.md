# Cycle 008 — Sending an OTP is async, ordered correctly, logged, and refuses disabled accounts: spec

## Contents

- L20 Summary
- L29 Why this matters
- L43 Product decision baked into this plan
- L52 Repo facts you need
- L69 Current state (as of the planning commit; plans 001/003/007 will have modified parts)
- L99 Scope
- L115 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 008 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2
- **Effort**: M
- **Risk**: LOW–MED (changes the `ISMSService` interface; there is one caller)
- **Depends on**: docs/archive/cycles/003-remove-committed-secrets-and-redact-logs/, docs/archive/cycles/005-establish-test-and-ci-baseline/, docs/archive/cycles/007-rate-limit-otp-and-login/
- **Category**: bug
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

`SendOtpCode` is an anonymous endpoint. Today it:
1. sends the SMS **synchronously**, using the obsolete `HttpWebRequest` with the
   default 100-second timeout, *inside* an async request. A slow SMS gateway
   therefore ties up thread-pool threads and can stall the whole server.
2. sends the SMS **before** the user exists and before the code is stored. If
   storing fails, the user receives a code that can never work.
3. swallows every exception with `_ = exc;` and reports "OTP service not
   connected", so real failures (DB errors, null references) leave no trace in
   the logs.
4. ignores `User.Active` and lets archived users through. A disabled account can
   still log in by OTP or by password.

## Product decision baked into this plan

**Archived (`IsArchive = true`) or inactive (`Active = false`) accounts cannot
log in** (OTP or password). Today the archived path is accidental: account
creation fails silently and the code carries on with the archived user. If the
business wants "re-register an archived phone number as a fresh account", that
is a separate feature: the Identity unique-username check would need to change.
Record this decision in your report so the maintainer can confirm it.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- .NET 8. `IHttpClientFactory` is already used by `src/Logiwall.Infrastructure/Providers/HttpProvider.cs`
  (`httpClientFactory.CreateClient()`), and Infrastructure references
  `Microsoft.Extensions.Http`. Register a **named client** for SMS.
- `System.Text.Json` is the backend serializer. `Newtonsoft.Json` is used only
  in `SMSService`; this plan removes that use. Do **not** remove the package
  reference (plan 015 does that).
- Services get `ILogger<T>` through constructor injection; the logging
  abstractions are available transitively. `HttpProvider` uses static Serilog
  `Log`. **Don't copy that**; use `ILogger<T>`.
- `ServiceResponse<T>`/`Succuss` is a misspelled wire contract and must not be
  renamed. Don't add `.resx` keys.
- Test harness: `tests/Logiwall.IntegrationTests` (plan 005) with
  `Infrastructure/FakeSmsService.cs`, which implements `ISMSService`.

## Current state (as of the planning commit; plans 001/003/007 will have modified parts)

`src/Logiwall.Application/Abstractions/Tools/ISMSService.cs`:
```csharp
public interface ISMSService
{
    string SendSMS(string Message, string Destination);
}
```

`src/Logiwall.Infrastructure/Services/Tools/SMSService.cs`, after plan 003:
- constructor takes `AppDbContext` (unused) and `IOptions<SmsOptions>`;
- `SendSMS` → `SendSMSPersiaFava`, which uses `HttpWebRequest`,
  `GetRequestStream()`/`GetResponse()` (synchronous, response never disposed),
  a form-urlencoded body with fields `api_key`, `receiver_number`, `note_arr[]`
  and `sender_number`, and parses the response JSON with Newtonsoft into
  `SMSPersiaFavaResult { bool result; string error; }`. It returns `"OK"` when
  `result` is true, and otherwise the error text.

`src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` (`SendOtpCodeAsync`), in its original order:
1. generate code → 2. load settings → 3. `smsService.SendSMS(...)` (result ignored) → 4. find or create the user → 5. add a `UserOtp` row and save → 6. set the `("OTP","otp")`/`("OTP","timestamp")` tokens → 7. return success.
It is all wrapped in `try { ... } catch (Exception exc) { _ = exc; return ...OtpServiceIsNotConnected... }`.
`LogUserLoginAsync` (lines 162–185) also swallows with `_ = exc;`.

`src/Logiwall.Domain/Entities/Identity/User.cs`: `bool IsArchive`, `bool Active`.

`src/Logiwall.Server/Controllers/Identity/IdentityController.cs`:
- `ConfirmationOtpCode` (lines 52-72) finds the user by phone, returns 400 if null, then calls the service.
- `GetToken` (lines 187-221) calls `signInManager.PasswordSignInAsync(...)` and throws `UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)))` on failure.

## Scope

**In scope**:
- `src/Logiwall.Application/Abstractions/Tools/ISMSService.cs`
- `src/Logiwall.Infrastructure/Services/Tools/SMSService.cs`
- `src/Logiwall.Infrastructure/Services/DependencyInjection.cs` (named HttpClient registration)
- `src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs`
- `src/Logiwall.Server/Controllers/Identity/IdentityController.cs` (`ConfirmationOtpCode` and `GetToken`: the disabled-account check only)
- `tests/Logiwall.IntegrationTests/Infrastructure/FakeSmsService.cs`
- `tests/Logiwall.IntegrationTests/Identity/OtpSendPathTests.cs` (create)

**Out of scope**:
- Changing the SMS provider, adding retries, queueing SMS in the background.
- Removing the `UserOtp` table or its row (it's an audit trail; keep writing it).
- Throttling logic (plan 007), the token issuers (`GenerateJwtToken`), and the `SignUp` endpoint (plan 009).

## Maintenance notes

- If SMS volume grows, move sending to a background queue. `SendSmsAsync` is already the right seam for that.
- The 10 s timeout is deliberate: the user is waiting on the request.
- The decision on archived/inactive accounts needs product confirmation (see the top of this plan).
