# Cycle 009 — Production exposes no Swagger, sample endpoints, raw exception text or account-deleting sign-up, and its proxy and CORS trust are explicit: spec

## Contents

- L20 Summary
- L29 Why this matters
- L42 Repo facts you need
- L57 Current state
- L105 Scope
- L122 Operator actions (before or with the deploy)
- L129 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 009 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2
- **Effort**: S
- **Risk**: LOW–MED (CORS and forwarded headers must match the real deployment; see the operator notes)
- **Depends on**: docs/archive/cycles/005-establish-test-and-ci-baseline/
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Several bit-bp template defaults are still active in production:
- Swagger UI and the full API schema are served in every environment.
- An anonymous sample endpoint, `/api/minimal-api-sample/{x}`, is live. Minimal APIs are not covered by `MapControllers().RequireAuthorization()`.
- The anonymous `SignUp` action deletes any existing account with that username whose email is not confirmed, then recreates it with the caller's password. Email confirmation is commented out, so no account can ever become confirmed.
- CORS allows any origin.
- Forwarded headers are set to `ForwardedHeaders.All`, with a custom `X-Host` header and no trusted proxies configured, so host and scheme handling is wrong or spoofable depending on topology.
- Services copy `ex.Message` (EF/SQL error text) into API responses.

Each fix is small. Together they shrink the attack surface and stop internal
details from leaking to clients.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- The real UI is the separate React app `Logiwall-Front`. Its endpoint list
  (`Logiwall-Front/src/hooks/use-api-endpoints.ts`) does **not** use `SignUp`, the
  sample endpoint or Swagger. The Blazor client's `IIdentityController`
  (`src/Client/Logiwall.Client.Core/Controllers/Identity/IIdentityController.cs`)
  has `SignUp` commented out, so deleting the server action breaks no client.
- Production runs on Azure App Service (Linux), provisioned by
  `src/Logiwall.Iac/AppStack.cs`. App Service terminates TLS and forwards
  `X-Forwarded-For` and `X-Forwarded-Proto` from its own front ends, and the app is
  not reachable except through them.
- Services use constructor injection. `ILogger<T>` is available.
- `ServiceResponse<T>`/`Succuss` is a misspelled wire contract. Don't rename it.

## Current state

`src/Logiwall.Server/Program.Middlewares.cs`:
```csharp
// 0.0.0.0 origins are essential for the proper functioning of BlazorHybrid's WebView, while localhost:4030 is a prerequisite for BlazorWebAssemblyStandalone testing.
app.UseCors(options => options.AllowAnyOrigin()
    .AllowAnyHeader().AllowAnyMethod());
...
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.InjectJavascript($"/swagger/swagger-utils.js?v={Environment.TickCount64}");
});

app.MapGet("/api/minimal-api-sample/{routeParameter}", (string routeParameter, [FromQuery] string queryStringParameter) => new
{
    RouteParameter = routeParameter,
    QueryStringParameter = queryStringParameter
}).WithTags("Test");
```

`src/Logiwall.Server/Program.Services.cs:31-35`:
```csharp
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.ForwardedHostHeaderName = "X-Host";
});
```

`src/Logiwall.Server/Controllers/Identity/IdentityController.cs:74-110`: the
`SignUp` action, starting with the `/// <summary> By leveraging summary tags...`
comment and ending with the commented `//await SendConfirmationEmail(...)` line.
Its core is:
```csharp
var existingUser = await userManager.FindByNameAsync(signUpRequest.Email!);
...
    else
    {
        var deleteResult = await userManager.DeleteAsync(existingUser);
```

Raw exception text returned to clients:
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`, `CreateOrderAsync` catch: `Message = ex.Message`
- `src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs`, catch: `Message = ex.Message` (twice: inside `Data` and at the top level)
- `src/Logiwall.Infrastructure/Services/People/PersonService.cs`, `UpdateUserProfileAsync` catch: `Message = exc.Message`

## Scope

**In scope**:
- `src/Logiwall.Server/Program.Middlewares.cs`
- `src/Logiwall.Server/Program.Services.cs` (the forwarded-headers block only)
- `src/Logiwall.Server/appsettings.json` (add the `Cors`, `ForwardedHeaders` and `Swagger` sections)
- `src/Logiwall.Server/appsettings.Development.json` (`Swagger:Enabled=true`)
- `src/Logiwall.Server/Controllers/Identity/IdentityController.cs` (delete `SignUp` only)
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`, `PaymentService.cs`, `src/Logiwall.Infrastructure/Services/People/PersonService.cs` (the catch blocks only)
- `tests/Logiwall.IntegrationTests/Hardening/ProductionDefaultsTests.cs` (create)

**Out of scope**:
- Accepting `access_token` from the query string and cookie (`Program.Services.cs`, `OnMessageReceived`). The Blazor client and SignalR may depend on it; see the maintenance notes.
- The payment `CallbackUrl` validation (plan 012).
- Health checks (disabled by config) and the `AllowedHosts` value (operator setting).
- The commented-out email/refresh/reset code in `IdentityController` (plan 015).

## Operator actions (before or with the deploy)

1. Set `Cors__AllowedOrigins__0=<frontend origin>` (and `__1`, ... for others) in App Service configuration.
2. On Azure App Service, set `ForwardedHeaders__TrustAllProxies=true`. Without it, client IPs are wrong, which also breaks plan 007's per-IP rate limits.
3. Set `AllowedHosts` to the real host names.
4. If Swagger is wanted in a staging environment, set `Swagger__Enabled=true` there only.

## Maintenance notes

- Accepting `access_token` from the query string exposes tokens in proxy and
  access logs. If neither the Blazor client nor any SignalR hub needs it,
  remove the query-string fallback in a later change.
- New anonymous endpoints must be deliberate: minimal APIs need explicit `.RequireAuthorization()`.
