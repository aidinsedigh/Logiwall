# Plan 008: Sending an OTP is async, ordered correctly, logged, and refuses disabled accounts

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Application/Abstractions/Tools Logiwall-API/src/Logiwall.Infrastructure/Services/Tools Logiwall-API/src/Logiwall.Infrastructure/Services/Identity Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs`
> Expected: plan 001 (CreateAsync without password), plan 003 (SMSService now
> reads `SmsOptions`; IPPanel method deleted; `AddInfrastructure(IConfiguration)`),
> and plan 007 (cooldown, attempts, FixedTimeEquals, rate-limit attributes).
> Before you start, open both service files and confirm those changes are
> present. If they are not, STOP: this plan builds on them.

## Status

- **Priority**: P2
- **Effort**: M
- **Risk**: LOW–MED (changes the `ISMSService` interface; there is one caller)
- **Depends on**: docs/work/001-audit-remediation/003-remove-committed-secrets-and-redact-logs.md, docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md, docs/work/001-audit-remediation/007-rate-limit-otp-and-login.md
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

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

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

## Git workflow

- Branch `advisor/008-otp-send-path`. Commits such as `Make SMS sending async with a timeout`. Do NOT push.

## Steps

### Step 1: Async SMS contract and implementation

1. `ISMSService`:
   ```csharp
   public interface ISMSService
   {
       /// <returns>"OK" on success, otherwise a provider error description.</returns>
       Task<string> SendSmsAsync(string message, string destination, CancellationToken cancellationToken = default);
   }
   ```
2. In `DependencyInjection.AddInfrastructure`, add
   `services.AddHttpClient("sms", c => c.Timeout = TimeSpan.FromSeconds(10));`.
3. Rewrite `SMSService` so that:
   - the constructor takes `IHttpClientFactory`, `IOptions<SmsOptions>` and `ILogger<SMSService>`, and the unused `AppDbContext` is removed;
   - `SendSmsAsync` creates `httpClientFactory.CreateClient("sms")` and posts
     `new FormUrlEncodedContent(new[] { new KeyValuePair<string,string>("api_key", opts.ApiKey), ("receiver_number", destination), ("note_arr[]", message), ("sender_number", opts.SenderNumber) })`
     to `opts.Url`. Dispose the response with `using`;
   - it reads the body as a string, and deserializes it with
     `System.Text.Json.JsonSerializer.Deserialize<PersiaFavaResult>(body)`, where
     `PersiaFavaResult` is a private record with `[JsonPropertyName("result")] bool Result`
     and `[JsonPropertyName("error")] string? Error`;
   - it returns `"OK"` if `Result` is true, and otherwise `Error ?? $"HTTP {(int)response.StatusCode}"`;
   - on `HttpRequestException`, `TaskCanceledException` or `JsonException`, it logs
     with `logger.LogWarning(ex, "SMS send failed for {Destination}", MaskPhone(destination))`
     and returns `"ERROR"`. `MaskPhone` keeps only the last 4 digits. **Never log the message text: it contains the OTP.**
   - Remove `using Newtonsoft.Json;`, `using System.Net;` and the old methods.

**Verify**: `dotnet build Logiwall.Server.slnf` fails **only** on `AuthenticationService` and `FakeSmsService` (they still call `SendSMS`). Note the errors; the next steps fix them.

### Step 2: Reorder and harden `SendOtpCodeAsync`

1. Inject `ILogger<AuthenticationService> logger`.
2. Restructure `SendOtpCodeAsync` into this order, keeping plan 007's
   cooldown check as the first step:
   1. cooldown check (from plan 007; unchanged);
   2. find the user. If the user exists and `IsArchive || Active == false`,
      return `Succuss = false` with message `"Account is disabled."`, and do not
      send an SMS;
   3. if the user is null, create it (plan 001's block, minus the `IsArchive`
      branch, which step 2 now handles). If creation fails, log a warning with
      the `IdentityResult` error codes and return the existing failure;
   4. generate the code; store the `UserOtp` row and the three tokens (`otp`, `timestamp`, `attempts`);
   5. `var smsResult = await smsService.SendSmsAsync(message ?? code, requestDto.PhoneNumber);`;
      if `smsResult != "OK"`, log a warning (masked phone) and return the existing
      `OtpServiceIsNotConnected` failure. The stored code stays valid, which is fine;
   6. return success.
3. Replace both `_ = exc;` catch bodies (`SendOtpCodeAsync` and `LogUserLoginAsync`)
   with `logger.LogError(exc, "<method name> failed");`, keeping the existing
   return values.

**Verify**: `grep -n "_ = exc" src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` → no output, and `grep -n "SendSmsAsync" ...AuthenticationService.cs` → exactly 1 match, at a line **after** `SetAuthenticationTokenAsync(user, "OTP", "otp"`.

### Step 3: Refuse disabled accounts at login

In `IdentityController`:
- `ConfirmationOtpCode`: after the null-user check, add
  `if (user.IsArchive || user.Active is false) return BadRequest(new ServiceResponse<ConfirmationOtpResponseDto> { Succuss = false, Message = "Account is disabled." });`
- `GetToken`: before `PasswordSignInAsync`, look up the user with
  `userManager.FindByNameAsync(signInRequest.UserName!)`. If it is non-null and
  disabled, throw the same `UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)))`
  used for a bad password, so the response doesn't reveal that the account exists.

**Verify**: build → errors remain only in `FakeSmsService`.

### Step 4: Update the fake and add tests

1. `FakeSmsService`: implement `SendSmsAsync`. Add a settable `string NextResult { get; set; } = "OK";`, and keep recording messages.
2. Create `tests/Logiwall.IntegrationTests/Identity/OtpSendPathTests.cs`:
   - `Sms_failure_returns_400_but_code_is_stored`: set the fake to `"ERROR"` → 400. The `UserTokens` table has `("OTP","otp")` for the user. Reset the fake in `finally`.
   - `Archived_user_cannot_request_otp`: create a user via OTP login, set `IsArchive = true` through `AppDbContext` → SendOtpCode returns 400, and no new SMS is recorded.
   - `Inactive_user_cannot_get_token`: register a user with a password, set `Active = false` → GetToken returns 401.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all pass.

## Test plan

See Step 4. All earlier tests (plans 005 and 007) must still pass.

## Done criteria

- [ ] `dotnet build Logiwall.Server.slnf` exits 0
- [ ] `grep -rn "HttpWebRequest\|JsonConvert" src/Logiwall.Infrastructure` → no output
- [ ] `grep -rn "SendSMS(" src tests` → no output (the old method is gone everywhere)
- [ ] `dotnet test tests/Logiwall.IntegrationTests` → 0 failed
- [ ] No log statement includes the SMS message or the OTP code (reviewer inspects; `grep -n "Log.*message" src/Logiwall.Infrastructure/Services/Tools/SMSService.cs` → no output)
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- Plans 003 or 007 have not landed (see the drift check).
- Something other than `AuthenticationService` calls `ISMSService` (`grep -rn "ISMSService" src`). Report it; don't widen scope.
- The PersiaFava response format differs from `{ "result": bool, "error": string }`
  (check against any recorded response in a local `src/Logiwall.Server/Logs/*.txt`,
  if one exists). **Don't paste log lines into your report.**

## Maintenance notes

- If SMS volume grows, move sending to a background queue. `SendSmsAsync` is already the right seam for that.
- The 10 s timeout is deliberate: the user is waiting on the request.
- The decision on archived/inactive accounts needs product confirmation (see the top of this plan).
