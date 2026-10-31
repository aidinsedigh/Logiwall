# Cycle 013 — plan

## Contents

- L30 Git workflow
- L34 STOP conditions
- L40 Commands you will need
- L47 Tasks
  - L49 Task 1: `HttpProvider` correctness
  - L61 Task 2: Token cache
  - L86 Task 3: Cache the settings
  - L94 Task 4: Tests
- L108 Test plan
- L112 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Providers Logiwall-API/src/Logiwall.Application/Abstractions/Providers/IHttpProvider.cs Logiwall-API/src/Logiwall.Shared/Dtos/Providers/HttpProviderRequest.cs Logiwall-API/src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs`
> Expected from earlier plans: 003 (log methods in HttpProvider redacted; providers use options),
> 011 (`ProviderType` override in MyTipaxProvider), and possibly 012 (`GetPaymentInfoAsync` implemented in
> Sep724Provider). Find the excerpts by content. If plan 003's redacted log methods
> are **not** present, STOP: this plan must not reintroduce body logging.

## Git workflow

- Branch `advisor/013-http-provider`. Commit per step. Do NOT push.

## STOP conditions

- Plan 003's redaction is missing (see the drift check).
- A caller outside `src/Logiwall.Infrastructure/Providers` uses `IHttpProvider` (grep). Report it before changing the interface.
- MyTipax returns a token that is tied to the login user in a way that makes sharing it across requests unsafe. Check the DTOs and any comments. If unclear, cache only the `token` step, not `login`, and note it in your report.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Tasks

### Task 1: `HttpProvider` correctness

For **each** of the four verb methods:
1. Add a final optional parameter `CancellationToken cancellationToken = default` to the interface and the implementation. Pass it to `SendAsync`/`GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`/`PostAsJsonAsync`/`PutAsJsonAsync`/`ReadFromJsonAsync`.
2. Timeout: `client.Timeout = TimeSpan.FromSeconds(request.Timeout ?? 30);`. This fixes the unit bug and sets a default.
3. `using var response = ...` (declare it, then assign it, so it is disposed at method end).
4. Decoding: if `decodeHandler` is set, use it. Otherwise, if `response.Content.Headers.ContentType?.MediaType` contains `json`, `try { result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken); } catch (JsonException ex) { logger.LogWarning(ex, "Non-JSON or invalid JSON from {Uri} ({Status})", request.Uri, (int)response.StatusCode); result = default; }`. If it isn't JSON, set `result = default` and log a warning with the status.
5. Keep the existing `failHandler` condition (`StatusCode is not OK && failHandler is not null`).
6. Replace static `Log.*` with an injected `ILogger<HttpProvider> logger` (primary-constructor parameter). Keep plan 003's redaction exactly.

**Verify**: build → exit 0; `grep -n "FromMilliseconds\|Serilog" src/Logiwall.Infrastructure/Providers/HttpProvider.cs` → no output; `grep -c "using var response" src/Logiwall.Infrastructure/Providers/HttpProvider.cs` → 4 (or 1 if you unified the methods).

### Task 2: Token cache

1. Create `ProviderTokenCache` (a singleton) wrapping `IMemoryCache`:
   ```csharp
   public sealed class ProviderTokenCache(IMemoryCache cache)
   {
       public async Task<string?> GetOrCreateAsync(string key, Func<Task<(string? Token, TimeSpan Lifetime)>> factory)
       { /* return cached; else call factory; cache only non-empty tokens for Lifetime minus 30s safety margin (minimum 30s) */ }
       public void Invalidate(string key) => cache.Remove(key);
   }
   ```
   Register it with `services.AddMemoryCache(); services.TryAddSingleton<ProviderTokenCache>();` in `AddInfrastructure`.
2. `Sep724Provider.GetTokenAsync`: wrap it in `tokenCache.GetOrCreateAsync("sep724:token", ...)`, using `TimeSpan.FromSeconds(result.ExpireIn)`. If `ExpireIn <= 0`, use 5 minutes.
3. Fail handler: replace the instance counter with a parameter. Make a private overload
   `GetPaymentTokenAsync(PaymentTokenRequestDto dto, int attempt)`. The public method calls it with `attempt: 1`, and
   the fail handler retries **once** (`attempt < 2`) after calling `tokenCache.Invalidate("sep724:token")`.
   Null-guard it: `if (response?.Status == 401 && attempt < 2)`. Delete the `tryToGetToken` and `maxTryToGetToken` fields.
   Because the fail handler is a delegate, it can be a lambda that captures `attempt`.
4. `MyTipaxProvider`: cache the **login** token (the one used for the inquiry) under `"mytipax:login"`, with a
   10-minute lifetime (there is no expiry in the API). Restructure it so that a cache hit makes **no** token or login calls,
   and a miss makes exactly token → login. If the inquiry returns 401, invalidate both keys and retry once.
   Delete the instance `token` field.

**Verify**: build → exit 0; `grep -n "tryToGetToken" src/Logiwall.Infrastructure/Providers/Sep724Provider.cs` → no output.

### Task 3: Cache the settings

In `AppSettingService`, inject `IMemoryCache` and cache the built `AppSettingsDto` under `"app-settings"` for 5 minutes
(`cache.GetOrCreateAsync`). The reflection mapping stays the same. Add the XML doc comment
`/// <remarks>Cached for 5 minutes; DB edits take effect after expiry or restart.</remarks>`.

**Verify**: build → exit 0.

### Task 4: Tests

`tests/Logiwall.UnitTests/Providers/HttpProviderTests.cs` (a fake `HttpMessageHandler` that returns configured responses and records requests):
1. `Html_502_returns_default_not_throw`: the handler returns 502 with `text/html` → `PostAsync` returns null and doesn't throw.
2. `Timeout_is_in_seconds`: the handler delays 2 s, the request has `Timeout = 1` → throws `TaskCanceledException` in about 1 s, not in 1 ms. Assert elapsed ≥ 900 ms.
3. `Cancellation_is_propagated`: a cancelled token → `TaskCanceledException` (or `OperationCanceledException`).
4. `Header_values_are_not_logged`: use a capturing `ILogger<HttpProvider>` (a small list logger). Send a header `Authorization: Basic secret-value`, then assert that no log message contains `secret-value`.

Provider token caching (unit tests, using a hand-written `IHttpProvider` fake that counts calls per URI, plus an `IAppSettingService` fake):
5. `Sep_token_fetched_once_for_two_payment_links`.
6. `MyTipax_second_quote_makes_only_inquiry_call`.

**Verify**: `dotnet test tests/Logiwall.UnitTests` → all pass; the integration tests still pass.

## Test plan

See Task 4. Everything from earlier plans must still pass.

## Done criteria

- [ ] Build exits 0, and all unit and integration tests pass (≥6 new unit tests)
- [ ] Greps in Tasks 1–2 return no output
- [ ] `grep -rn "Log\.\(Information\|Error\|Warning\)" src/Logiwall.Infrastructure/Providers` → no output (ILogger only)
- [ ] Only in-scope files changed
- [ ] this cycle's `tasks.md` status row updated
