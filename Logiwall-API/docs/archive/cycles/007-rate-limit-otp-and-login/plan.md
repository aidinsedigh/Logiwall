# Cycle 007 — plan

## Contents

- L30 Git workflow
- L34 STOP conditions
- L47 Commands you will need
- L55 Tasks
  - L57 Task 1: Validate phone numbers at the DTO boundary
  - L67 Task 2: Cap verification attempts and add a resend cooldown
  - L130 Task 3: Per-IP rate limits on the anonymous auth endpoints
  - L155 Task 4: Tests
- L172 Test plan
- L176 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs Logiwall-API/src/Logiwall.Server/Program.Services.cs Logiwall-API/src/Logiwall.Server/Program.Middlewares.cs Logiwall-API/src/Logiwall.Shared/Dtos/Identity`
> Expected changes from earlier plans: plan 001 (the `CreateAsync` block in
> `SendOtpCodeAsync`), plan 003 (`AddInfrastructure(configuration)`), and plan 005
> (cert path in `Program.Services.cs:167` and `IdentityController.cs:306`). Any
> other drift in the lines quoted below is a STOP condition.

## Git workflow

- Branch `advisor/007-rate-limit-otp`. Commits such as `Cap OTP verification attempts`. Do NOT push.

## STOP conditions

- The production deployment sits behind a reverse proxy. If so,
  `RemoteIpAddress` is the proxy's address and every user shares one bucket.
  Check `src/Logiwall.Iac/AppStack.cs`: if the app runs on Azure App Service, the
  front end forwards `X-Forwarded-For`, and `UseForwardedHeaders` must trust it.
  Plan 009 configures `KnownNetworks`. **If plan 009 has not landed yet, keep the
  limits as written, and put a prominent note in your report that per-IP limits
  are only correct after plan 009.** Don't widen forwarded-header trust here.
- Any existing plan-005 test starts failing because of the cooldown (it would
  mean a test reuses a phone). Report which test; don't loosen the cooldown.
- `RegularExpressionAttribute` breaks the Blazor client build (it shouldn't, since the attribute is in the BCL).

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` | all pass |

## Tasks

### Task 1: Validate phone numbers at the DTO boundary

On `PhoneNumber` in both `SendOtpRequestDto` and `ConfirmaionOtpRequestDto`, add
`[RegularExpression(@"^09\d{9}$", ErrorMessage = nameof(AppStrings.RegularExpressionAttribute_ValidationError))]`.
First check that this resource key exists with
`grep -n "RegularExpressionAttribute_ValidationError" src/Logiwall.Shared/Resources/AppStrings.Designer.cs`.
If it doesn't exist, use `[RegularExpression(@"^09\d{9}$")]` without `ErrorMessage`.

**Verify**: build → exit 0.

### Task 2: Cap verification attempts and add a resend cooldown

In `AuthenticationService`:

1. Add private constants:
   ```csharp
   private const int MaxOtpAttempts = 5;
   private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(2);
   private static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);
   ```
   Use `OtpValidity` where the code currently says `DateTimeOffset.Now.AddMinutes(2)` (line 59).
2. **Resend cooldown.** At the start of `SendOtpCodeAsync`, *before*
   `GenerateRandomCode()` and the SMS send, look up the user by name. If the user
   exists, read the token `("OTP","timestamp")`. If it parses, and
   `parsed - OtpValidity + OtpResendCooldown > DateTimeOffset.Now` (meaning the
   last code was issued less than 60 s ago), return:
   ```csharp
   new ServiceResponse<bool> { Data = false, Succuss = false, Message = "Please wait before requesting a new code." }
   ```
   without sending an SMS. You may then reuse this `user` variable further down,
   instead of the second `FindByNameAsync`, as long as behavior is otherwise unchanged.
3. **When issuing a new code**, next to the two existing
   `SetAuthenticationTokenAsync` calls, also call
   `SetAuthenticationTokenAsync(user, "OTP", "attempts", "0")`.
4. **Attempt cap in `ConfirmationOtpCodeAsync`.** Replace the comparison block quoted in "Current state" with:
   ```csharp
   var storedOTP = await userManager.GetAuthenticationTokenAsync(user, "OTP", "otp");
   var attemptsText = await userManager.GetAuthenticationTokenAsync(user, "OTP", "attempts");
   _ = int.TryParse(attemptsText, out var attempts);

   if (storedOTP is null || attempts >= MaxOtpAttempts || CodesEqual(storedOTP, requestDto.Code) is false)
   {
       attempts++;
       if (storedOTP is not null && attempts >= MaxOtpAttempts)
       {
           // Too many wrong guesses: burn the code so the remaining keyspace can't be searched.
           await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "otp");
           await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "timestamp");
       }
       await userManager.SetAuthenticationTokenAsync(user, "OTP", "attempts", attempts.ToString(CultureInfo.InvariantCulture));

       return new ServiceResponse<ConfirmationOtpResponseDto>
       {
           Succuss = false,
           Message = localizer.GetString(nameof(AppStrings.OtpCodeIsNotCorrect))
       };
   }
   ```
   and add:
   ```csharp
   private static bool CodesEqual(string expected, string? actual)
       => actual is not null && CryptographicOperations.FixedTimeEquals(
              Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
   ```
   (`System.Security.Cryptography` is already imported. Add `using System.Text;` if needed.)
5. On success, next to the existing two `RemoveAuthenticationTokenAsync` calls (lines 128–129),
   also remove `("OTP","attempts")`.
6. `ConfirmationOtpCodeAsync` currently dereferences `user` without a null check
   (line 96). The controller checks for a null user first
   (`IdentityController.cs:54-58`), so leave that as it is.

**Verify**: build → exit 0, and `grep -n "storedOTP != requestDto.Code" src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` → no output.

### Task 3: Per-IP rate limits on the anonymous auth endpoints

1. In `Program.Services.cs`, inside `ConfigureServices`, after `services.AddCors();`, add:
   ```csharp
   services.AddRateLimiter(options =>
   {
       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
       options.AddPolicy("otp-send", ctx => RateLimitPartition.GetFixedWindowLimiter(
           ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
           _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:OtpSendPerMinute", 5), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
       options.AddPolicy("auth-verify", ctx => RateLimitPartition.GetFixedWindowLimiter(
           ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
           _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue("RateLimits:AuthVerifyPerMinute", 20), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
   });
   ```
   Usings: `Microsoft.AspNetCore.RateLimiting` and `System.Threading.RateLimiting`.
2. In `Program.Middlewares.cs`, add `app.UseRateLimiter();` immediately after `app.UseAuthorization();`.
3. In `IdentityController`, add `[EnableRateLimiting("otp-send")]` to `SendOtpCode`, and
   `[EnableRateLimiting("auth-verify")]` to `ConfirmationOtpCode` and `GetToken`.
4. Tests share one IP, so in `LogiwallApiFactory.ConfigureWebHost` add
   `builder.UseSetting("RateLimits:OtpSendPerMinute", "1000"); builder.UseSetting("RateLimits:AuthVerifyPerMinute", "1000");`.
   One dedicated test (Task 4, case 4) creates its own factory with the default limits.

**Verify**: build → exit 0, and `grep -rn "EnableRateLimiting" src/Logiwall.Server/Controllers` → 3 matches.

### Task 4: Tests

`tests/Logiwall.IntegrationTests/Identity/OtpThrottlingTests.cs`, `[Collection("api")]`:
1. `Sixth_guess_fails_even_with_the_right_code`: send an OTP and read the real
   code. Submit 5 wrong codes (e.g. `"00000"`; if that happens to equal the real
   code, use `"00001"`). Then submit the real code → 400. Assert that `UserTokens`
   no longer holds an `("OTP","otp")` row for the user.
2. `Correct_code_within_attempts_succeeds_and_resets_counter`: 2 wrong codes, then the right code → 200 with a token.
3. `Resend_within_cooldown_is_rejected_without_sms`: SendOtpCode twice back to back → the second returns 400 and `FakeSmsService` saw exactly one message for that phone. (Add a `CountFor(phone)` helper to the fake if needed.)
4. `Ip_limit_returns_429`: build a separate `LogiwallApiFactory` *without*
   the relaxed settings. Make 6 `SendOtpCode` calls with 6 different phones → the 6th is 429.
   (This factory still needs the container's connection string. Reuse the
   fixture's `ConnectionString`.)
5. `Malformed_phone_is_rejected`: SendOtpCode with `"12345"` → 400, and no SMS recorded.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all tests pass, including 5 new ones.

## Test plan

See Task 4. Plan 005's 8 tests must still pass. In particular, its `LoginWithOtpAsync` helper must keep working under the new cooldown, since each test uses a fresh phone.

## Done criteria

- [ ] `dotnet build Logiwall.Server.slnf` exits 0
- [ ] `dotnet test tests/Logiwall.IntegrationTests` → 0 failed, ≥13 passed
- [ ] `grep -rn "AddRateLimiter" src/Logiwall.Server` → 1 match; `grep -rn "UseRateLimiter" src/Logiwall.Server` → 1 match
- [ ] `grep -n "FixedTimeEquals" src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` → 1 match
- [ ] Only in-scope files changed
- [ ] this cycle's `tasks.md` status row updated
