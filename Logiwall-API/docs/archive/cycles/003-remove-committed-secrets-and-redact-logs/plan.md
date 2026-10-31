# Cycle 003 — plan

## Contents

- L40 ⚠ Secret-handling rule for the executor
- L50 Git workflow
- L56 STOP conditions
- L63 Commands you will need
- L72 Tasks
  - L74 Task 1: Add options classes and bind them
  - L126 Task 2: Providers read credentials from options
  - L149 Task 3: Remove credential properties from `AppSettingsDto`
  - L159 Task 4: Strip secrets and dead code from `SMSService`
  - L174 Task 5: Stop logging credentials in `HttpProvider`
  - L199 Task 6: Blank the seeded values and delete the rows
  - L221 Task 7: Configuration skeleton, connection string, gitignore and log file
- L255 Test plan
- L265 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
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

## ⚠ Secret-handling rule for the executor

This plan touches files that contain **live credentials**. You must:
- never print, copy, quote or log a credential value in your output, commit
  messages, comments, tests or report. When you need to show a line, replace
  the value with `<redacted>`;
- delete values instead of moving them. The new configuration keys get
  **empty-string** values in `appsettings.json`. Real values are set by an
  operator through user-secrets or Key Vault, never by you.

## Git workflow

- Branch: `advisor/003-remove-committed-secrets`
- Commit messages are imperative and contain **no values**, e.g. `Read provider credentials from configuration`.
- Do NOT push.

## STOP conditions

- You would have to print a credential value to proceed. That is never necessary; stop and report.
- `dotnet ef migrations add` changes the model snapshot or produces non-empty `Up`/`Down`.
- `AppSettingsDto` properties you are deleting are referenced outside `src/Logiwall.Infrastructure/Providers` (`grep -rn` them first).
- The `string.Format` body in `SMSService` uses different parameter names or order than shown. The provider contract may differ, so report rather than guess.

## Commands you will need

| Purpose | Command | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0 |
| EF tool | `cd src/Logiwall.Server && dotnet tool restore` | success |
| Add migration | `cd src/Logiwall.Server && dotnet ef migrations add RemoveProviderSecretsFromAppSettings --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` | "Done." |
| Secret scan | see Task 7 | no matches |

## Tasks

### Task 1: Add options classes and bind them

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

### Task 2: Providers read credentials from options

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

### Task 3: Remove credential properties from `AppSettingsDto`

Delete these properties from `src/Logiwall.Shared/Dtos/Global/AppSettingsDto.cs`:
`SepConnectUsername`, `SepConnectPassword`, `SepConnectClient`, `SepConnectAuthorization`,
`MyTipaxUsernameToken`, `MyTipaxPasswordToken`, `MyTipaxUsernameLogin`, `MyTipaxPasswordLogin`.
`AppSettingService` skips unknown keys (`property != null` check at line 27),
so leftover DB rows can't break it.

**Verify**: build → exit 0, and `grep -rn "SepConnectPassword" src --include=*.cs | grep -v Migrations` → no output.

### Task 4: Strip secrets and dead code from `SMSService`

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

### Task 5: Stop logging credentials in `HttpProvider`

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

### Task 6: Blank the seeded values and delete the rows

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

### Task 7: Configuration skeleton, connection string, gitignore and log file

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
- [ ] All three secret-scan greps in Task 7 return no matches
- [ ] `git ls-files src/Logiwall.Server/Logs` is empty, and `.gitignore` contains `**/Logs/`
- [ ] `grep -rn "SendSMSIPPanel" src` → no matches
- [ ] `AppDbContextModelSnapshot.cs` is unchanged
- [ ] No credential value appears in the diff (`git diff 594e1c5 -- . | grep -i "<any original value>"` cannot be run without knowing the values, so the reviewer will inspect the diff)
- [ ] this cycle's `tasks.md` status row updated
