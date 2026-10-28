# Cycle 008 — plan

## Contents

- L31 Git workflow
- L35 STOP conditions
- L43 Commands you will need
- L50 Tasks
  - L52 Task 1: Async SMS contract and implementation
  - L81 Task 2: Reorder and harden `SendOtpCodeAsync`
  - L104 Task 3: Refuse disabled accounts at login
  - L116 Task 4: Update the fake and add tests
- L126 Test plan
- L130 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Application/Abstractions/Tools Logiwall-API/src/Logiwall.Infrastructure/Services/Tools Logiwall-API/src/Logiwall.Infrastructure/Services/Identity Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs`
> Expected: plan 001 (CreateAsync without password), plan 003 (SMSService now
> reads `SmsOptions`; IPPanel method deleted; `AddInfrastructure(IConfiguration)`),
> and plan 007 (cooldown, attempts, FixedTimeEquals, rate-limit attributes).
> Before you start, open both service files and confirm those changes are
> present. If they are not, STOP: this plan builds on them.

## Git workflow

- Branch `advisor/008-otp-send-path`. Commits such as `Make SMS sending async with a timeout`. Do NOT push.

## STOP conditions

- Plans 003 or 007 have not landed (see the drift check).
- Something other than `AuthenticationService` calls `ISMSService` (`grep -rn "ISMSService" src`). Report it; don't widen scope.
- The PersiaFava response format differs from `{ "result": bool, "error": string }`
  (check against any recorded response in a local `src/Logiwall.Server/Logs/*.txt`,
  if one exists). **Don't paste log lines into your report.**

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Tasks

### Task 1: Async SMS contract and implementation

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

### Task 2: Reorder and harden `SendOtpCodeAsync`

1. Inject `ILogger<AuthenticationService> logger`.
2. Restructure `SendOtpCodeAsync` into this order, keeping plan 007's
   cooldown check as the first step:
   1. cooldown check (from plan 007; unchanged);
   2. find the user. If the user exists and `IsArchive || Active == false`,
      return `Succuss = false` with message `"Account is disabled."`, and do not
      send an SMS;
   3. if the user is null, create it (plan 001's block, minus the `IsArchive`
      branch, which task 2 now handles). If creation fails, log a warning with
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

### Task 3: Refuse disabled accounts at login

In `IdentityController`:
- `ConfirmationOtpCode`: after the null-user check, add
  `if (user.IsArchive || user.Active is false) return BadRequest(new ServiceResponse<ConfirmationOtpResponseDto> { Succuss = false, Message = "Account is disabled." });`
- `GetToken`: before `PasswordSignInAsync`, look up the user with
  `userManager.FindByNameAsync(signInRequest.UserName!)`. If it is non-null and
  disabled, throw the same `UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)))`
  used for a bad password, so the response doesn't reveal that the account exists.

**Verify**: build → errors remain only in `FakeSmsService`.

### Task 4: Update the fake and add tests

1. `FakeSmsService`: implement `SendSmsAsync`. Add a settable `string NextResult { get; set; } = "OK";`, and keep recording messages.
2. Create `tests/Logiwall.IntegrationTests/Identity/OtpSendPathTests.cs`:
   - `Sms_failure_returns_400_but_code_is_stored`: set the fake to `"ERROR"` → 400. The `UserTokens` table has `("OTP","otp")` for the user. Reset the fake in `finally`.
   - `Archived_user_cannot_request_otp`: create a user via OTP login, set `IsArchive = true` through `AppDbContext` → SendOtpCode returns 400, and no new SMS is recorded.
   - `Inactive_user_cannot_get_token`: register a user with a password, set `Active = false` → GetToken returns 401.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all pass.

## Test plan

See Task 4. All earlier tests (plans 005 and 007) must still pass.

## Done criteria

- [ ] `dotnet build Logiwall.Server.slnf` exits 0
- [ ] `grep -rn "HttpWebRequest\|JsonConvert" src/Logiwall.Infrastructure` → no output
- [ ] `grep -rn "SendSMS(" src tests` → no output (the old method is gone everywhere)
- [ ] `dotnet test tests/Logiwall.IntegrationTests` → 0 failed
- [ ] No log statement includes the SMS message or the OTP code (reviewer inspects; `grep -n "Log.*message" src/Logiwall.Infrastructure/Services/Tools/SMSService.cs` → no output)
- [ ] this cycle's `tasks.md` status row updated
