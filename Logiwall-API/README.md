# Logiwall API

## What this is

Logiwall API is the backend for Logiwall's parcel-shipping app. It provides OTP login,
customers and contacts, orders, courier quotes (MyTipax) and payment (SEP 724).

The real UI is the React app in `../Logiwall-Front/`. Agents: read `CLAUDE.md` and
`instruction.md` first.

## Prerequisites

* .NET 8 SDK 8.0.301 or a later 8.0 SDK. `global.json` rolls forward inside .NET 8 only.
* Docker. You need it for a local SQL Server and for the integration tests.
* Optional: SQL Server LocalDB or a local SQL Server instead of Docker.

## First run

1. Start SQL Server in Docker:

   ```bash
   docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=<choose one> -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
   ```

2. Set the connection string with user-secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:SqlServerConnectionString" "Server=localhost,1433;Database=LogiwallDb;User Id=sa;Password=<choose one>;TrustServerCertificate=True" --project src/Logiwall.Server
   ```

3. Apply the migrations (from `src/Logiwall.Server`):

   ```bash
   cd src/Logiwall.Server
   dotnet tool restore
   dotnet ef database update --project ../Logiwall.Infrastructure --startup-project .
   ```

4. Run the server and open Swagger at `http://localhost:5030/swagger`:

   ```bash
   dotnet run --project src/Logiwall.Server
   ```

The server accepts the committed `IdentityCertificate.pfx` only in Development. That
certificate expired in January 2025, so bearer tokens signed with it fail validation.
Replace it locally with a new certificate (see the error text in `Program.Services.cs`).

## Configuration

Set secrets with `dotnet user-secrets set "<key>" "<value>" --project src/Logiwall.Server`
in development, and with Key Vault or App Service settings in Azure (`Providers__Sep724__Password`).
`appsettings.json` keeps these keys with empty values.

| Key | Secret? | Where it is set | Used by |
|---|---|---|---|
| `ConnectionStrings:SqlServerConnectionString` | Yes, if it has a password | user-secrets / Key Vault | `Program.Services.cs` (`AppDbContext`) |
| `Providers:Sep724:Username` | Yes | user-secrets / Key Vault | `Sep724Provider` |
| `Providers:Sep724:Password` | Yes | user-secrets / Key Vault | `Sep724Provider` |
| `Providers:Sep724:Client` | Yes | user-secrets / Key Vault | `Sep724Provider` |
| `Providers:Sep724:BasicAuthorization` | Yes | user-secrets / Key Vault | `Sep724Provider` |
| `Providers:Sep724:Terminal` | No (environment-specific) | appsettings / Key Vault | `Sep724Provider` |
| `Providers:MyTipax:TokenUsername` | Yes | user-secrets / Key Vault | `MyTipaxProvider` |
| `Providers:MyTipax:TokenPassword` | Yes | user-secrets / Key Vault | `MyTipaxProvider` |
| `Providers:MyTipax:LoginUsername` | Yes | user-secrets / Key Vault | `MyTipaxProvider` |
| `Providers:MyTipax:LoginPassword` | Yes | user-secrets / Key Vault | `MyTipaxProvider` |
| `Sms:PersiaFava:Url` | No | appsettings / Key Vault | `SMSService` |
| `Sms:PersiaFava:ApiKey` | Yes | user-secrets / Key Vault | `SMSService` |
| `Sms:PersiaFava:SenderNumber` | No | appsettings / Key Vault | `SMSService` |

The options classes are in `src/Logiwall.Infrastructure/Options/`.

Non-secret runtime settings are rows in the database table `AppSettings`. Migrations seed
them, and `src/Logiwall.Infrastructure/Services/Global/AppSettingService.cs` reads them into
`AppSettingsDto`:

| `AppSettings` row |
|---|
| `LoginOtpText` |
| `SepConnectUrl` |
| `SepConnectScope` |
| `SepPayBaseUrl` |
| `MyTipaxBaseUrl` |
| `MyTipaxInquiryVersion` |
| `MyTipaxIban` |
| `MyTipaxShare` |
| `LogiwallIban` |
| `LogiwallShare` |

Never put a credential in an `AppSettings` row or a migration.

## Build and test

```bash
dotnet build Logiwall.Server.slnf
dotnet test tests/Logiwall.UnitTests
dotnet test tests/Logiwall.IntegrationTests
```

Do not build `Logiwall.sln` or `Logiwall.Web.slnf`. Both contain `Logiwall.Client.Maui`, which
needs MAUI workloads.

The integration tests need Docker. They start SQL Server with Testcontainers and apply every
migration. EF InMemory and SQLite do not work: the model uses the sequence `dbo.OrderSequence`,
`HasDefaultValueSql` with `FORMAT`, and filtered unique indexes.

CI runs the same commands: `../.github/workflows/api-ci.yml`.

## Migrations

Migrations live in `src/Logiwall.Infrastructure/Data/Migrations`. Run the EF commands from
`src/Logiwall.Server`:

```bash
cd src/Logiwall.Server
dotnet tool restore
dotnet ef migrations add <Name> --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations
dotnet ef database update --project ../Logiwall.Infrastructure --startup-project .
```

## Deploy

`src/Logiwall.Iac` is a Pulumi stack for Azure: App Service, Azure SQL, Key Vault and
Application Insights. See [`src/Logiwall.Iac/Readme.md`](src/Logiwall.Iac/Readme.md).
Nothing runs migrations automatically. Run `dotnet ef database update` (or a generated
script) against each environment before you deploy code that needs a new migration.

## Project layout

| Project | Contents |
|---|---|
| `src/Logiwall.Server` | ASP.NET Core host: controllers (`api/[controller]/[action]`), Identity and bearer auth, Swagger, Serilog file logs (`Logs/`) |
| `src/Logiwall.Application` | Service interfaces only (`Abstractions/**`) |
| `src/Logiwall.Infrastructure` | `AppDbContext` (namespace `Logiwall.Server.Data`), migrations, services, providers (SEP, MyTipax, SMS), Mapperly mappers, options |
| `src/Logiwall.Domain` | EF entities |
| `src/Logiwall.Shared` | DTOs, enums, resources, exceptions (shared with the clients) |
| `src/Client/*` | bit-bp template Blazor/MAUI clients. The server hosts `Logiwall.Client.Web` |
| `src/Logiwall.Iac` | Pulumi Azure stack |
| `tests/Logiwall.UnitTests` | xUnit unit tests |
| `tests/Logiwall.IntegrationTests` | xUnit integration tests on Testcontainers SQL Server |

Work plans are in `docs/work/`. See `docs/README.md` for the docs layout.
