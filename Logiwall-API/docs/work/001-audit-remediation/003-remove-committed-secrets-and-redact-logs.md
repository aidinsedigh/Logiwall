# Plan 003: No third-party credentials live in source, migrations or logs; they come from configuration

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure Logiwall-API/src/Logiwall.Server/appsettings.json Logiwall-API/src/Logiwall.Server/Program.Services.cs Logiwall-API/src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs Logiwall-API/.gitignore`
> Changes made by plans 001/002 (to `AuthenticationService.cs`, `CustomerService.cs`,
> `OrderService.cs`, `PaymentService.cs`, a new migration) are expected. Any
> other mismatch with the excerpts below is a STOP condition.
>
> **Amended 2026-10-27 — Central Package Management**: package versions now live in
> `Logiwall-API/Directory.Packages.props`. A `*.csproj` has `<PackageReference Include="X" />`
> with no `Version`. To add `Microsoft.Extensions.Options.ConfigurationExtensions`, add
> `<PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />`
> to `Directory.Packages.props`. Then add the `PackageReference` with no `Version` to
> `Logiwall.Infrastructure.csproj`.

## Status

- **Priority**: P1 (security: leaked credentials)
- **Effort**: M (the code part is S; credential rotation is operator work)
- **Risk**: MED (the deployed environments must receive the new configuration keys before this code runs)
- **Depends on**: none (if plan 001 has landed, your new migration simply gets a later timestamp)
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

## ⚠ Secret-handling rule for the executor

This plan touches files that contain **live credentials**. You must:
- never print, copy, quote or log a credential value in your output, commit
  messages, comments, tests or report. When you need to show a line, replace
  the value with `<redacted>`;
- delete values instead of moving them. The new configuration keys get
  **empty-string** values in `appsettings.json`. Real values are set by an
  operator through user-secrets or Key Vault, never by you.

## Why this matters

The repo is pushed to a GitHub remote and contains working credentials for the
payment gateway (SEP), the courier (MyTipax), two SMS providers and the SQL
Server database. On top of that, `HttpProvider` logs every outbound
`Authorization` header and request body at Information level, so gateway
credentials and tokens end up in plaintext log files. One of those files is
committed. Removing the values from the current tree does **not** un-leak them,
so an operator must also rotate every credential (see "Operator actions"). This
plan makes sure that after rotation the new values never land in git or logs
again.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall` and contains `Logiwall-API/` and
  `Logiwall-Front/` (do not touch the front end). Paths are relative to
  `Logiwall-API/`, and commands run from there unless stated otherwise.
- .NET 8, ASP.NET Core, EF Core 8 SQL Server. Build only
  `src/Logiwall.Server/Logiwall.Server.csproj`, never `Logiwall.sln` (MAUI).
- Configuration: `src/Logiwall.Server/appsettings.json` and user-secrets.
  `UserSecretsId` is already set at `Logiwall.Server.csproj:99`. In production
  the IaC project provisions an Azure Key Vault
  (`src/Logiwall.Iac/AppStack.cs:204-273`).
- Infrastructure services are registered in
  `src/Logiwall.Infrastructure/Services/DependencyInjection.cs` via
  `services.AddInfrastructure()`, called from `src/Logiwall.Server/Program.Services.cs:83`.
- Runtime "settings" for providers are read from the DB table `AppSettings`
  (key/value rows) by `src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs`.
  It uses reflection to fill `src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs`
  (one string property per key). Those rows were seeded by raw SQL in migrations.
- Migrations: `src/Logiwall.Infrastructure/Data/Migrations`, namespace
  `Logiwall.Infrastructure.Data.Migrations`. The EF tool manifest is in
  `src/Logiwall.Server/.config/dotnet-tools.json`, so run EF commands from
  `src/Logiwall.Server`.

## Current state (credential locations, values redacted)

| # | Location | What it is |
|---|---|---|
| A | `src/Logiwall.Infrastructure/Data/Migrations/20240907210035_AddSepInAppSetting.cs:17,19,23,27` | `INSERT INTO AppSettings ... VALUES ('SepConnectUsername' / 'SepConnectPassword' / 'SepConnectAuthorization' / 'SepConnectClient', '<redacted>')`: SEP gateway login, password, Basic-auth header value and client ID |
| B | `src/Logiwall.Infrastructure/Data/Migrations/20240921075422_AddCitiesAndOtherConstants.cs:20,22,24,26` | Same pattern for `MyTipaxUsernameToken`, `MyTipaxPasswordToken`, `MyTipaxPasswordLogin`, `MyTipaxUsernameLogin`: MyTipax API credentials |
| C | `src/Logiwall.Infrastructure/Services/Tools/SMSService.cs:29-60` | Method `SendSMSIPPanel` (never called). It contains an IPPanel SMS username/password in live code (around lines 52–53) and again in comments (lines 31–44), a raw-IP HTTP endpoint (line 47), and a personal mobile number |
| D | `src/Logiwall.Infrastructure/Services/Tools/SMSService.cs:68-70` | `SendSMSPersiaFava` posts to a hardcoded `http://` URL. Its form body embeds the PersiaFava `api_key` value and a sender number via `string.Format("api_key=<redacted>&receiver_number={0}&note_arr[]={1}&sender_number={2}", ...)` |
| E | `src/Logiwall.Server/appsettings.json:3` | `ConnectionStrings:SqlServerConnectionString` containing `User Id=` and `Password=` for a SQL login |
| F | `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs:13` | `private readonly string? terminal = "<redacted terminal id>";`, the merchant terminal ID (an identifier, not a secret, but environment-specific) |
| G | `src/Logiwall.Infrastructure/Providers/HttpProvider.cs:287-308` | `LogRequestDetails` logs every header as `{Key}: {Value}` plus `JsonSerializer.Serialize(request.Body)`. `LogResponseDetails` logs full response headers and body. Both log at Information level |
| H | `src/Logiwall.Server/Logs/log-20240929.txt` | Tracked in git. It contains output from G, including SEP credentials |
| I | `.gitignore` | Ignores `*.log`, but not `Logs/` or `*.txt` logs, so Serilog's `Logs/log-YYYYMMDD.txt` files can be committed |

How the providers read credentials today:
- `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs:84-97` (`GetTokenAsync`):
  ```csharp
  var appSettings = await appSettingService.GetAllSettingsAsync();
  var header = new List<(string, string)>
  {
      new ("Authorization", $"Basic {appSettings.SepConnectAuthorization}")
  };
  var body = new List<KeyValuePair<string, string>>
  {
      new ("grant_type", grantType),
      new ("username" , appSettings.SepConnectUsername),
      new ("password" , appSettings.SepConnectPassword),
      new ("scope" , appSettings.SepConnectScope),
      new ("client" , appSettings.SepConnectClient),
  };
  ```
- `src/Logiwall.Infrastructure/Providers/PostalServices/MyTipaxProvider.cs:50-55` and `71-78`
  read `appSettings.MyTipaxUsernameToken`, `MyTipaxPasswordToken`,
  `MyTipaxUsernameLogin` and `MyTipaxPasswordLogin`.
- `HttpProvider.cs:287-308` today:
  ```csharp
  private void LogRequestDetails<TBody, TRequest>(HttpProviderRequest<TBody, TRequest> request)
  where TRequest : IHttpRequest
  {
      Log.Information("Request to {Uri} with headers: {Headers}",
          request.Uri, request.HeaderParameters != null ?
          string.Join(", ", request.HeaderParameters.Select(h => $"{h.Key}: {h.Value}")): string.Empty);

      if (request.Body != null)
      {
          Log.Information("Request body: {Body}", JsonSerializer.Serialize(request.Body));
      }
  }

  private async Task LogResponseDetails(HttpResponseMessage response)
  {
      var responseBody = await response.Content.ReadAsStringAsync();

      Log.Information("Response from {Uri}: Status Code: {StatusCode}, Headers: {Headers}, Body: {Body}",
          response.RequestMessage?.RequestUri,
          response.StatusCode,
          response.Headers,
          responseBody);
  }
  ```

Conventions: Infrastructure services use constructor injection
(`Sep724Provider.cs:17-22`). Follow that pattern when injecting `IOptions<T>`.

## Commands you will need

| Purpose | Command | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0 |
| EF tool | `cd src/Logiwall.Server && dotnet tool restore` | success |
| Add migration | `cd src/Logiwall.Server && dotnet ef migrations add RemoveProviderSecretsFromAppSettings --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` | "Done." |
| Secret scan | see Step 7 | no matches |

## Scope

**In scope**:
- `src/Logiwall.Infrastructure/Options/Sep724Options.cs` (create)
- `src/Logiwall.Infrastructure/Options/MyTipaxOptions.cs` (create)
- `src/Logiwall.Infrastructure/Options/SmsOptions.cs` (create)
- `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`
- `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs`
- `src/Logiwall.Infrastructure/Providers/PostalServices/MyTipaxProvider.cs`
- `src/Logiwall.Infrastructure/Providers/HttpProvider.cs` (only the two log methods)
- `src/Logiwall.Infrastructure/Services/Tools/SMSService.cs`
- `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj` (only if Step 1 needs a package)
- `src/Logiwall.Infrastructure/Data/Migrations/20240907210035_AddSepInAppSetting.cs` (blank values only)
- `src/Logiwall.Infrastructure/Data/Migrations/20240921075422_AddCitiesAndOtherConstants.cs` (blank credential values only)
- `src/Logiwall.Infrastructure/Data/Migrations/<new>_RemoveProviderSecretsFromAppSettings.cs` (+ `.Designer.cs`)
- `src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs`
- `src/Logiwall.Server/Program.Services.cs` (only the `AddInfrastructure` call)
- `src/Logiwall.Server/appsettings.json`
- `.gitignore`
- `src/Logiwall.Server/Logs/log-20240929.txt` (untrack it with `git rm --cached`; do not delete the local file)

**Out of scope**:
- Rewriting git history (filter-repo/BFG). That requires a force-push and is an
  operator decision, listed under "Operator actions".
- Making SMS async or changing its behavior (plan 008). Only remove secrets and
  dead code here.
- Retries and timeouts in `HttpProvider` (plan 013).
- `IdentityCertificatePassword` in `appsettings.json`. It belongs to the
  template's public development certificate; startup rejects that certificate
  outside Development (`Program.Services.cs:170-174`).
- IBAN rows (`LogiwallIban`, `MyTipaxIban`). They are payout destinations,
  business configuration rather than credentials.
- `Logiwall-Front/` (it has its own tracked `.env`; see Operator actions).

## Git workflow

- Branch: `advisor/003-remove-committed-secrets`
- Commit messages are imperative and contain **no values**, e.g. `Read provider credentials from configuration`.
- Do NOT push.

## Steps

### Step 1: Add options classes and bind them

Create three files in `src/Logiwall.Infrastructure/Options/` (namespace `Logiwall.Infrastructure.Options`):

```csharp
public class Sep724Options
{
    public const string SectionName = "Providers:Sep724";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    /// <summary>Value placed after "Basic " in the token request's Authorization header.</summary>
    public string BasicAuthorization { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
}

public class MyTipaxOptions
{
    public const string SectionName = "Providers:MyTipax";
    public string TokenUsername { get; set; } = string.Empty;
    public string TokenPassword { get; set; } = string.Empty;
    public string LoginUsername { get; set; } = string.Empty;
    public string LoginPassword { get; set; } = string.Empty;
}

public class SmsOptions
{
    public const string SectionName = "Sms:PersiaFava";
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
}
```

In `DependencyInjection.cs`, change the signature to
`public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`
and add at the top of the method:
```csharp
services.Configure<Sep724Options>(configuration.GetSection(Sep724Options.SectionName));
services.Configure<MyTipaxOptions>(configuration.GetSection(MyTipaxOptions.SectionName));
services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
```
In `Program.Services.cs:83`, change `services.AddInfrastructure();` to
`services.AddInfrastructure(configuration);`. The local `configuration` variable
already exists there.

If `Configure<T>(IConfigurationSection)` doesn't resolve, add
`<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />`
to `Logiwall.Infrastructure.csproj`.

**Verify**: build → exit 0.

### Step 2: Providers read credentials from options

- `Sep724Provider`: inject `IOptions<Sep724Options> sep724Options` through the
  constructor and store `.Value`. Delete the `terminal` field (line 13) and use
  `options.Terminal`. In `GetTokenAsync`, use `options.BasicAuthorization`,
  `options.Username`, `options.Password` and `options.Client`. Keep `SepConnectScope`
  and the URLs coming from `appSettings`: they are not secret.
- At the start of `GetTokenAsync`, if any of `Username`, `Password` or
  `BasicAuthorization` is empty, throw
  `new InvalidOperationException("Providers:Sep724 credentials are not configured.")`.
- `MyTipaxProvider`: add an `IOptions<MyTipaxOptions>` constructor parameter and
  pass the other two to `base(...)` unchanged. Use `TokenUsername`/`TokenPassword`
  in `GetTokenAsync` and `LoginUsername`/`LoginPassword` in `LoginAsync`. Apply the
  same empty-check with `"Providers:MyTipax credentials are not configured."`.

Note: `OrderService.cs:90-94` creates `MyTipaxProvider` by reflection, resolving
each constructor parameter from `IServiceProvider`. `IOptions<MyTipaxOptions>`
is registered in DI, so that still works. Don't change `OrderService`.

**Verify**:
- Build → exit 0.
- `grep -n "SepConnectUsername\|SepConnectPassword\|SepConnectAuthorization\|SepConnectClient\|MyTipaxUsername\|MyTipaxPassword" -r src/Logiwall.Infrastructure/Providers` → no output.

### Step 3: Remove credential properties from `AppSettingsDto`

Delete these properties from `src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs`:
`SepConnectUsername`, `SepConnectPassword`, `SepConnectClient`, `SepConnectAuthorization`,
`MyTipaxUsernameToken`, `MyTipaxPasswordToken`, `MyTipaxUsernameLogin`, `MyTipaxPasswordLogin`.
`AppSettingService` skips unknown keys (`property != null` check at line 27),
so leftover DB rows can't break it.

**Verify**: build → exit 0, and `grep -rn "SepConnectPassword" src --include=*.cs | grep -v Migrations` → no output.

### Step 4: Strip secrets and dead code from `SMSService`

1. Delete the whole `SendSMSIPPanel` method (lines 29–60, including its commented block) and the commented `SaveSMSLog` block (lines 102–122).
2. Inject `IOptions<SmsOptions>` into the constructor and keep `.Value` in a field.
3. In `SendSMSPersiaFava`, replace the hardcoded URL with `smsOptions.Url`. Build
   the body as
   `$"api_key={Uri.EscapeDataString(smsOptions.ApiKey)}&receiver_number={Uri.EscapeDataString(DestinationAddress)}&note_arr[]={Uri.EscapeDataString(Message)}&sender_number={Uri.EscapeDataString(smsOptions.SenderNumber)}"`.
   Keep everything else (HttpWebRequest and so on) as it is; plan 008 rewrites it.
4. Remove `using RestSharp;` if nothing else in the file uses it.

**Verify**:
- Build → exit 0.
- `grep -n "api_key=" src/Logiwall.Infrastructure/Services/Tools/SMSService.cs` → exactly one match, and it contains `Uri.EscapeDataString(smsOptions.ApiKey)`.
- `grep -n "SendSMSIPPanel\|RestClient" src/Logiwall.Infrastructure/Services/Tools/SMSService.cs` → no output.

### Step 5: Stop logging credentials in `HttpProvider`

Replace the two methods shown in "Current state" with:
```csharp
private void LogRequestDetails<TBody, TRequest>(HttpProviderRequest<TBody, TRequest> request)
where TRequest : IHttpRequest
{
    // Never log header values or bodies: they carry provider credentials and tokens.
    Log.Information("Outbound request to {BaseAddress}{Uri} with header names: {HeaderNames}",
        request.BaseAddress, request.Uri,
        request.HeaderParameters is null ? string.Empty : string.Join(", ", request.HeaderParameters.Select(h => h.Key)));
}

private Task LogResponseDetails(HttpResponseMessage response)
{
    Log.Information("Outbound response from {Uri}: {StatusCode}",
        response.RequestMessage?.RequestUri, (int)response.StatusCode);
    return Task.CompletedTask;
}
```
Keep the call sites (`LogRequestDetails(request);` and `await LogResponseDetails(response);`)
unchanged. If `System.Text.Json` is no longer used in the file, remove the `using System.Text.Json;` line.

**Verify**: build → exit 0, and `grep -n "Serialize(request.Body)\|ReadAsStringAsync" src/Logiwall.Infrastructure/Providers/HttpProvider.cs` → no output.

### Step 6: Blank the seeded values and delete the rows

1. In `20240907210035_AddSepInAppSetting.cs`, replace the value literal of each
   of the four credential `INSERT`s (lines 17, 19, 23, 27) with `''`. Example of the
   resulting shape: `VALUES ('SepConnectPassword', '')`. Change nothing else.
2. In `20240921075422_AddCitiesAndOtherConstants.cs`, do the same for the four
   `MyTipax*Username*` / `MyTipax*Password*` `INSERT`s (lines 20, 22, 24, 26).
   Leave the URL, version, share and IBAN rows untouched.
3. Add a migration named `RemoveProviderSecretsFromAppSettings` (command above).
   The generated `Up`/`Down` must be empty. Set `Up` to:
   ```csharp
   migrationBuilder.Sql(@"
       DELETE FROM [dbo].[AppSettings]
       WHERE [KeyName] IN ('SepConnectUsername','SepConnectPassword','SepConnectAuthorization','SepConnectClient',
                           'MyTipaxUsernameToken','MyTipaxPasswordToken','MyTipaxUsernameLogin','MyTipaxPasswordLogin');");
   ```
   and leave `Down` empty with the comment `// Irreversible: credentials now come from configuration.`

**Verify**:
- Build → exit 0.
- `git diff --stat -- src/Logiwall.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` → no changes.

### Step 7: Configuration skeleton, connection string, gitignore and log file

1. In `src/Logiwall.Server/appsettings.json`:
   - Replace the `SqlServerConnectionString` value with the credential-free
     local default
     `"Server=(localdb)\\MSSQLLocalDB;Database=LogiwallDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"`.
     Add the comment `// Real environments: set via user-secrets / Key Vault.`
   - Add top-level sections with **empty** values:
     ```json
     "Providers": {
         "Sep724": { "Username": "", "Password": "", "Client": "", "BasicAuthorization": "", "Terminal": "" },
         "MyTipax": { "TokenUsername": "", "TokenPassword": "", "LoginUsername": "", "LoginPassword": "" }
     },
     "Sms": {
         "PersiaFava": { "Url": "", "ApiKey": "", "SenderNumber": "" }
     },
     ```
2. In `.gitignore`, under the existing `# Logiwall` section (line 224), add:
   ```
   **/Logs/
   *.pfx.local
   appsettings.*.local.json
   ```
3. Untrack the committed log: `git rm --cached src/Logiwall.Server/Logs/log-20240929.txt`.

**Verify** (run from `Logiwall-API/`):
- `git check-ignore src/Logiwall.Server/Logs/anything.txt` → prints the path (so it is ignored).
- `git ls-files src/Logiwall.Server/Logs` → no output.
- Secret scan. Each must print **no matches**:
  - `git grep -n "api_key=[A-Za-z0-9]" -- src`
  - `git grep -nE "Password=[^;\"]+;" -- src/Logiwall.Server/appsettings.json`
  - `git grep -nE "VALUES \('(SepConnect(Username|Password|Authorization|Client)|MyTipax(Username|Password)[A-Za-z]*)', ?'[^']+'\)" -- src`
- Build → exit 0.

## Test plan

There is no test project yet. Plan 005 adds the harness. Because credential
values are external, do not add tests that need them. Manual check, only if a
local DB and user-secrets are available (otherwise state in your report that it
was skipped):
`dotnet user-secrets set "Providers:Sep724:Username" "x" --project src/Logiwall.Server`.
Then confirm the app starts and that a request to `Order/SelectOption` fails
with a gateway error rather than a configuration error. Remove the secret afterwards.

## Done criteria

- [ ] Build exits 0
- [ ] All three secret-scan greps in Step 7 return no matches
- [ ] `git ls-files src/Logiwall.Server/Logs` is empty, and `.gitignore` contains `**/Logs/`
- [ ] `grep -rn "SendSMSIPPanel" src` → no matches
- [ ] `AppDbContextModelSnapshot.cs` is unchanged
- [ ] No credential value appears in the diff (`git diff 594e1c5 -- . | grep -i "<any original value>"` cannot be run without knowing the values, so the reviewer will inspect the diff)
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- You would have to print a credential value to proceed. That is never necessary; stop and report.
- `dotnet ef migrations add` changes the model snapshot or produces non-empty `Up`/`Down`.
- `AppSettingsDto` properties you are deleting are referenced outside `src/Logiwall.Infrastructure/Providers` (`grep -rn` them first).
- The `string.Format` body in `SMSService` uses different parameter names or order than shown. The provider contract may differ, so report rather than guess.

## Operator actions (humans; do these as soon as possible, independent of this code)

1. **Rotate**, through each provider's portal: the SEP gateway username/password,
   Basic credential and client; both MyTipax credential pairs; the PersiaFava
   SMS API key; the IPPanel SMS account password; and the SQL login whose
   password is in the committed connection string.
2. Put the new values in Key Vault / App Service configuration using the keys
   from Step 7 (`Providers__Sep724__Password`, and so on), and in local
   user-secrets for developers.
3. Delete old log files on production hosts. They contain credentials.
4. Check whether `https://github.com/aidinsedigh/Logiwall` is public. Either
   way, decide whether to purge history (`git filter-repo`), which needs a
   force-push and every collaborator to re-clone. Rotation is mandatory
   regardless.
5. Review `Logiwall-Front/.env`, which is tracked in git. It is outside this plan.
6. Confirm that the PersiaFava endpoint supports HTTPS, then set `Sms:PersiaFava:Url` to the https URL.

## Maintenance notes

- New provider credentials must go into an options class under
  `src/Logiwall.Infrastructure/Options`, never into `AppSettings` rows or
  migrations.
- Reviewers: scan diffs for `Authorization`, `password`, `api_key`, and for
  `Log.*` calls that include request or response bodies.
- Plan 013 replaces the static `Log` with `ILogger<HttpProvider>`. It must keep the no-values rule.
