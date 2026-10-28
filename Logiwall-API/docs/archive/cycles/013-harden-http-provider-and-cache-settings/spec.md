# Cycle 013 — Outbound HTTP calls time out, cancel, dispose and tolerate error bodies; settings and provider tokens are cached: spec

## Contents

- L18 Summary
- L27 Why this matters
- L43 Repo facts you need
- L75 Scope
- L93 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 013 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2
- **Effort**: M
- **Risk**: LOW–MED (token caching must respect expiry)
- **Depends on**: docs/archive/cycles/003-remove-committed-secrets-and-redact-logs/, docs/archive/cycles/005-establish-test-and-ci-baseline/
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
- `tests/Logiwall.UnitTests/Providers/HttpProviderTests.cs`, `tests/Logiwall.UnitTests/Global/AppSettingServiceTests.cs` (optional; see Task 4)

**Out of scope**:
- Polly or retry policies (a follow-up once timeouts exist).
- Changing the provider APIs or DTO shapes.
- Collapsing the four duplicated verb methods into one `SendAsync` core. That would be welcome but is optional; do it only if it keeps the diff smaller. Behavior must be identical.

## Maintenance notes

- If settings ever become editable at runtime, invalidate `"app-settings"` on write.
- With multiple app instances, each caches its own tokens. That's fine for OAuth tokens.
- The next step is a Polly retry with jitter for idempotent GETs only. Never automatically retry payment-token POSTs.
