# Cycle 003 — No third-party credentials live in source, migrations or logs; they come from configuration: spec

## Contents

- L20 Summary
- L29 Why this matters
- L41 Repo facts you need
- L64 Current state (credential locations, values redacted)
- L128 Scope
- L162 Operator actions (humans; do these as soon as possible, independent of this code)
- L179 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 003 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P1 (security: leaked credentials)
- **Effort**: M (the code part is S; credential rotation is operator work)
- **Risk**: MED (the deployed environments must receive the new configuration keys before this code runs)
- **Depends on**: none (if plan 001 has landed, your new migration simply gets a later timestamp)
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

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
- `src/Logiwall.Infrastructure/Logiwall.Infrastructure.csproj` (only if Task 1 needs a package)
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

## Operator actions (humans; do these as soon as possible, independent of this code)

1. **Rotate**, through each provider's portal: the SEP gateway username/password,
   Basic credential and client; both MyTipax credential pairs; the PersiaFava
   SMS API key; the IPPanel SMS account password; and the SQL login whose
   password is in the committed connection string.
2. Put the new values in Key Vault / App Service configuration using the keys
   from Task 7 (`Providers__Sep724__Password`, and so on), and in local
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
