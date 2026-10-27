# Plan 009: Production exposes no Swagger, sample endpoints, raw exception text or account-deleting sign-up, and its proxy and CORS trust are explicit

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Server/Program.Middlewares.cs Logiwall-API/src/Logiwall.Server/Program.Services.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems Logiwall-API/src/Logiwall.Infrastructure/Services/People/PersonService.cs Logiwall-API/src/Logiwall.Server/appsettings.json`
> Changes from plans 001–008 are expected: the rate limiter in Program.*,
> ownership predicates in OrderService/PaymentService, the cert path, and the
> config sections in appsettings. Re-locate each excerpt below by its **content**,
> not its line number. If an excerpt's content is gone, STOP.

## Status

- **Priority**: P2
- **Effort**: S
- **Risk**: LOW–MED (CORS and forwarded headers must match the real deployment; see the operator notes)
- **Depends on**: docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md
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

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

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

## Git workflow

- Branch `advisor/009-harden-production-defaults`. Commits per step. Do NOT push.

## Steps

### Step 1: Swagger only where enabled, and remove the sample endpoint

In `Program.Middlewares.cs`:
- Wrap `app.UseSwagger(); app.UseSwaggerUI(...)` in
  `if (configuration.GetValue<bool>("Swagger:Enabled")) { ... }`.
  The local `configuration` variable already exists at the top of the method.
- Delete the `app.MapGet("/api/minimal-api-sample/...` statement.

Add `"Swagger": { "Enabled": false }` to `appsettings.json`, and
`"Swagger": { "Enabled": true }` to `appsettings.Development.json`.

**Verify**: build → exit 0, and `grep -rn "minimal-api-sample" src` → no output.

### Step 2: Configurable CORS

Replace the `app.UseCors(...)` statement (keep its comment) with:
```csharp
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
app.UseCors(options =>
{
    if (allowedOrigins.Length > 0)
        options.WithOrigins(allowedOrigins);
    else
        options.AllowAnyOrigin(); // Fallback keeps existing deployments working until Cors:AllowedOrigins is configured.
    options.AllowAnyHeader().AllowAnyMethod();
});
if (allowedOrigins.Length == 0 && env.IsDevelopment() is false)
{
    app.Logger.LogWarning("Cors:AllowedOrigins is empty; CORS allows any origin. Configure the frontend origin(s).");
}
```
Add `"Cors": { "AllowedOrigins": [] }` to `appsettings.json`.

**Verify**: build → exit 0.

### Step 3: Explicit forwarded-header trust

Replace the `Configure<ForwardedHeadersOptions>` block with:
```csharp
services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only client IP and scheme are taken from the proxy; Host is never overridden by a header.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
    {
        // Safe only when the app is reachable exclusively through the platform proxy (e.g. Azure App Service).
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
    foreach (var proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
});
```
Add `"ForwardedHeaders": { "TrustAllProxies": false, "KnownProxies": [] }` to `appsettings.json`.

**Verify**: build → exit 0, and `grep -rn "X-Host" src/Logiwall.Server` → no output.

### Step 4: Delete the account-deleting `SignUp` action

Delete the `SignUp` method from `IdentityController`, together with its
`/// <summary>` comment block. Remove any `using` that becomes unused only
because of this; the build warnings will show which.

**Verify**: build → exit 0, and `grep -n "public async Task SignUp" src/Logiwall.Server/Controllers/Identity/IdentityController.cs` → no output.

### Step 5: Stop returning exception text

In each of the three catch blocks listed under "Current state":
- inject `ILogger<OrderService>` / `ILogger<PaymentService>` / `ILogger<PersonService>`
  through the constructor (add the parameter and field, matching the existing
  constructor style);
- log with `logger.LogError(ex, "<MethodName> failed");`;
- replace `ex.Message` / `exc.Message` with `"An unexpected error occurred."`.
  `PaymentService` has two occurrences; replace both.

**Verify**: build → exit 0, and `grep -rnE "Message = exc?\.Message" src/Logiwall.Infrastructure` → no output.

### Step 6: Tests

`tests/Logiwall.IntegrationTests/Hardening/ProductionDefaultsTests.cs`:
1. `Sample_endpoint_is_gone`: `GET api/minimal-api-sample/x?queryStringParameter=y` → not 200.
2. `Swagger_is_disabled_when_flag_is_false`: build a factory variant with
   `builder.UseSetting("Swagger:Enabled", "false")`, then `GET swagger/v1/swagger.json` → not 200.
   (The default test factory runs as Development, where it is enabled.)
3. `SignUp_endpoint_is_gone`: `POST api/Identity/SignUp` with `{}` → 404 or 405.
4. `Configured_cors_origin_is_echoed_and_others_are_not`: a factory variant with
   `Cors:AllowedOrigins:0 = https://app.example.test`. Send a preflight `OPTIONS api/Common/GetStaticDatas`
   with `Origin: https://app.example.test` and `Access-Control-Request-Method: GET` → the response has
   `Access-Control-Allow-Origin: https://app.example.test`. The same with `Origin: https://evil.example.test` → no
   `Access-Control-Allow-Origin` header.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all pass.

## Test plan

See Step 6. All earlier tests must stay green.

## Done criteria

- [ ] Build exits 0, and all integration tests pass
- [ ] The greps in Steps 1, 3, 4 and 5 return no output
- [ ] `appsettings.json` contains the `Swagger`, `Cors` and `ForwardedHeaders` sections
- [ ] Only in-scope files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- Something in `src/` or `Logiwall-Front/src` references `minimal-api-sample` or `Identity/SignUp` (grep first).
- Build errors show that `SignUp` was used by generated code (Bit.SourceGenerators proxies). Report it.
- Any excerpt in "Current state" can't be found by its content.

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
