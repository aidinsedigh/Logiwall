# Plan 013: Outbound HTTP calls time out, cancel, dispose and tolerate error bodies; settings and provider tokens are cached

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Providers Logiwall-API/src/Logiwall.Application/Abstractions/Providers/IHttpProvider.cs Logiwall-API/src/Logiwall.Shared/Dtos/Providers/HttpProviderRequest.cs Logiwall-API/src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs`
> Expected from earlier plans: 003 (log methods in HttpProvider redacted; providers use options),
> 011 (`ProviderType` override in MyTipaxProvider), and possibly 012 (`GetPaymentInfoAsync` implemented in
> Sep724Provider). Find the excerpts by content. If plan 003's redacted log methods
> are **not** present, STOP: this plan must not reintroduce body logging.

## Status

- **Priority**: P2
- **Effort**: M
- **Risk**: LOW–MED (token caching must respect expiry)
- **Depends on**: docs/work/001-audit-remediation/003-remove-committed-secrets-and-redact-logs.md, docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md
- **Category**: bug / perf
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Every courier quote and payment link makes several outbound HTTP calls through
`HttpProvider`, and each has problems:
- `HttpResponseMessage` is never disposed.
- There is no default timeout (100 s). The optional `Timeout` is documented as seconds but applied as milliseconds.
- `CancellationToken` is never passed through, so a client disconnect doesn't stop the work.
- The body is deserialized as JSON even on error responses, so an HTML 502 page becomes a `JsonException` and a 500.
- `Sep724Provider`'s 401 retry dereferences a possibly-null response.

On top of that, the DB settings table is fully re-read with reflection 2–3 times
per request, and a new courier and gateway OAuth token is fetched on **every**
call (MyTipax makes three sequential HTTP calls per quote). Fixing these cuts
latency and the load on the providers, and turns upstream errors into clean
failure responses.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- `IHttpProvider` (`src/Logiwall.Application/Abstractions/Providers/IHttpProvider.cs`) declares
  `PostAsync`/`PutAsync`/`GetAsync`/`DeleteAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null)`.
  Callers: `Sep724Provider` and `MyTipaxProvider` only (`grep -rn "HttpProvider\." src/Logiwall.Infrastructure`).
- `HttpProviderRequest` (`src/Logiwall.Shared/Dtos/Providers/HttpProviderRequest.cs`) has
  `int? Timeout`, documented as "Gets or sets Timeout in seconds". `HttpProvider.cs` lines 22, 92, 152 and 229 use
  `TimeSpan.FromMilliseconds(request.Timeout.Value)`. No caller sets `Timeout` today.
- `HttpProvider` is registered Transient (`DependencyInjection.cs`) and uses `IHttpClientFactory.CreateClient()`
  (the unnamed client), with per-request `DefaultRequestHeaders` on a fresh client. That is fine.
- It logs with static Serilog `Log`. Switch to `ILogger<HttpProvider>`. **Keep plan 003's rule: log
  method, URI, status and header *names* only; never header values or bodies.**
- `AppSettingService` (`src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs`) is Scoped. It loads
  every `AppSettings` row and fills `AppSettingsDto` by reflection.
- Token sources:
  - SEP: `Sep724Provider.GetTokenAsync()` → `GetTokenResponseDto { AccessToken, ExpireIn (seconds, JSON "expires_in"), TokenType }`.
  - MyTipax: `GetTokenAsync()` → `MyTipaxGetTokenResponseDto { Token }`, then `LoginAsync()` → `MyTipaxLoginResponseDto { Status, Message, PersianMessage, Token }`. Neither has an expiry. `MyTipaxProvider` caches `token` in an instance field, but it is resolved per request, so the cache is useless. `GetOrderInquiryAsync` calls `LoginAsync()` and then `GetHeaderAsync()`, and `LoginAsync` itself calls `GetHeaderAsync()`. Result: token, login, token (cached), inquiry.
- `Sep724Provider.GetPaymentTokanFailHandler` (sic):
  ```csharp
  if (response.Status == 401 && tryToGetToken < maxTryToGetToken)
  {
      tryToGetToken ++;
      return await GetPaymentTokenAsync(new PaymentTokenRequestDto { ... });
  }
  return response;
  ```
  `tryToGetToken` is an instance field (`private short tryToGetToken = 1;`).
- Tests: `tests/Logiwall.UnitTests` (plan 005). Write a fake `HttpMessageHandler` and wrap it in a
  real `IHttpClientFactory`. The simplest way is `new ServiceCollection().AddHttpClient().ConfigurePrimaryHttpMessageHandler(() => fake)`
  on the default client, then `BuildServiceProvider().GetRequiredService<IHttpClientFactory>()`.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Scope

**In scope**:
- `src/Logiwall.Application/Abstractions/Providers/IHttpProvider.cs`
- `src/Logiwall.Infrastructure/Providers/HttpProvider.cs`
- `src/Logiwall.Shared/Dtos/Providers/HttpProviderRequest.cs` (doc comment only, if needed)
- `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs` (`GetTokenAsync`, the fail handler)
- `src/Logiwall.Infrastructure/Providers/PostalServices/MyTipaxProvider.cs` (token/login flow)
- `src/Logiwall.Infrastructure/Providers/ProviderTokenCache.cs` (create)
- `src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs`
- `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`
- `tests/Logiwall.UnitTests/Providers/HttpProviderTests.cs`, `tests/Logiwall.UnitTests/Global/AppSettingServiceTests.cs` (optional; see Step 4)

**Out of scope**:
- Polly or retry policies (a follow-up once timeouts exist).
- Changing the provider APIs or DTO shapes.
- Collapsing the four duplicated verb methods into one `SendAsync` core. That would be welcome but is optional; do it only if it keeps the diff smaller. Behavior must be identical.

## Git workflow

- Branch `advisor/013-http-provider`. Commit per step. Do NOT push.

## Steps

### Step 1: `HttpProvider` correctness

For **each** of the four verb methods:
1. Add a final optional parameter `CancellationToken cancellationToken = default` to the interface and the implementation. Pass it to `SendAsync`/`GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`/`PostAsJsonAsync`/`PutAsJsonAsync`/`ReadFromJsonAsync`.
2. Timeout: `client.Timeout = TimeSpan.FromSeconds(request.Timeout ?? 30);`. This fixes the unit bug and sets a default.
3. `using var response = ...` (declare it, then assign it, so it is disposed at method end).
4. Decoding: if `decodeHandler` is set, use it. Otherwise, if `response.Content.Headers.ContentType?.MediaType` contains `json`, `try { result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken); } catch (JsonException ex) { logger.LogWarning(ex, "Non-JSON or invalid JSON from {Uri} ({Status})", request.Uri, (int)response.StatusCode); result = default; }`. If it isn't JSON, set `result = default` and log a warning with the status.
5. Keep the existing `failHandler` condition (`StatusCode is not OK && failHandler is not null`).
6. Replace static `Log.*` with an injected `ILogger<HttpProvider> logger` (primary-constructor parameter). Keep plan 003's redaction exactly.

**Verify**: build → exit 0; `grep -n "FromMilliseconds\|Serilog" src/Logiwall.Infrastructure/Providers/HttpProvider.cs` → no output; `grep -c "using var response" src/Logiwall.Infrastructure/Providers/HttpProvider.cs` → 4 (or 1 if you unified the methods).

### Step 2: Token cache

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

### Step 3: Cache the settings

In `AppSettingService`, inject `IMemoryCache` and cache the built `AppSettingsDto` under `"app-settings"` for 5 minutes
(`cache.GetOrCreateAsync`). The reflection mapping stays the same. Add the XML doc comment
`/// <remarks>Cached for 5 minutes; DB edits take effect after expiry or restart.</remarks>`.

**Verify**: build → exit 0.

### Step 4: Tests

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

See Step 4. Everything from earlier plans must still pass.

## Done criteria

- [ ] Build exits 0, and all unit and integration tests pass (≥6 new unit tests)
- [ ] Greps in Steps 1–2 return no output
- [ ] `grep -rn "Log\.\(Information\|Error\|Warning\)" src/Logiwall.Infrastructure/Providers` → no output (ILogger only)
- [ ] Only in-scope files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- Plan 003's redaction is missing (see the drift check).
- A caller outside `src/Logiwall.Infrastructure/Providers` uses `IHttpProvider` (grep). Report it before changing the interface.
- MyTipax returns a token that is tied to the login user in a way that makes sharing it across requests unsafe. Check the DTOs and any comments. If unclear, cache only the `token` step, not `login`, and note it in your report.

## Maintenance notes

- If settings ever become editable at runtime, invalidate `"app-settings"` on write.
- With multiple app instances, each caches its own tokens. That's fine for OAuth tokens.
- The next step is a Polly retry with jitter for idempotent GETs only. Never automatically retry payment-token POSTs.
