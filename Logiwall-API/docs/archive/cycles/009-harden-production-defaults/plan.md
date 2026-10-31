# Cycle 009 — plan

## Contents

- L32 Git workflow
- L36 STOP conditions
- L42 Commands you will need
- L49 Tasks
  - L51 Task 1: Swagger only where enabled, and remove the sample endpoint
  - L64 Task 2: Configurable CORS
  - L86 Task 3: Explicit forwarded-header trust
  - L110 Task 4: Delete the account-deleting `SignUp` action
  - L118 Task 5: Stop returning exception text
  - L130 Task 6: Tests
- L146 Test plan
- L150 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Server/Program.Middlewares.cs Logiwall-API/src/Logiwall.Server/Program.Services.cs Logiwall-API/src/Logiwall.Server/Controllers/Identity/IdentityController.cs Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems Logiwall-API/src/Logiwall.Infrastructure/Services/People/PersonService.cs Logiwall-API/src/Logiwall.Server/appsettings.json`
> Changes from plans 001–008 are expected: the rate limiter in Program.*,
> ownership predicates in OrderService/PaymentService, the cert path, and the
> config sections in appsettings. Re-locate each excerpt below by its **content**,
> not its line number. If an excerpt's content is gone, STOP.

## Git workflow

- Branch `advisor/009-harden-production-defaults`. Commits per step. Do NOT push.

## STOP conditions

- Something in `src/` or `Logiwall-Front/src` references `minimal-api-sample` or `Identity/SignUp` (grep first).
- Build errors show that `SignUp` was used by generated code (Bit.SourceGenerators proxies). Report it.
- Any excerpt in "Current state" can't be found by its content.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Tasks

### Task 1: Swagger only where enabled, and remove the sample endpoint

In `Program.Middlewares.cs`:
- Wrap `app.UseSwagger(); app.UseSwaggerUI(...)` in
  `if (configuration.GetValue<bool>("Swagger:Enabled")) { ... }`.
  The local `configuration` variable already exists at the top of the method.
- Delete the `app.MapGet("/api/minimal-api-sample/...` statement.

Add `"Swagger": { "Enabled": false }` to `appsettings.json`, and
`"Swagger": { "Enabled": true }` to `appsettings.Development.json`.

**Verify**: build → exit 0, and `grep -rn "minimal-api-sample" src` → no output.

### Task 2: Configurable CORS

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

### Task 3: Explicit forwarded-header trust

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

### Task 4: Delete the account-deleting `SignUp` action

Delete the `SignUp` method from `IdentityController`, together with its
`/// <summary>` comment block. Remove any `using` that becomes unused only
because of this; the build warnings will show which.

**Verify**: build → exit 0, and `grep -n "public async Task SignUp" src/Logiwall.Server/Controllers/Identity/IdentityController.cs` → no output.

### Task 5: Stop returning exception text

In each of the three catch blocks listed under "Current state":
- inject `ILogger<OrderService>` / `ILogger<PaymentService>` / `ILogger<PersonService>`
  through the constructor (add the parameter and field, matching the existing
  constructor style);
- log with `logger.LogError(ex, "<MethodName> failed");`;
- replace `ex.Message` / `exc.Message` with `"An unexpected error occurred."`.
  `PaymentService` has two occurrences; replace both.

**Verify**: build → exit 0, and `grep -rnE "Message = exc?\.Message" src/Logiwall.Infrastructure` → no output.

### Task 6: Tests

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

See Task 6. All earlier tests must stay green.

## Done criteria

- [ ] Build exits 0, and all integration tests pass
- [ ] The greps in Tasks 1, 3, 4 and 5 return no output
- [ ] `appsettings.json` contains the `Swagger`, `Cors` and `ForwardedHeaders` sections
- [ ] Only in-scope files changed
- [ ] this cycle's `tasks.md` status row updated
