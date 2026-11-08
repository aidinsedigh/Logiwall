# Backend release preparation: 2026-11-08

The API and shipping upgrade script were prepared locally before remote database access.
The subsequent authorized Liara database upgrade is recorded in [the execution report](database-upgrade-2026-11-08.md).
Application deployment and actual production configuration remain unverified.
The candidate uses base commit `0e16b72` plus the migration script fixes from this review.
Commit these fixes and assign the release tag before making the final production package.

## Artifacts

Local output: `artifacts/releases/2026-11-08-0e16b72/` (Git-ignored).

| File | Purpose |
|---|---|
| `shipping-upgrade.sql` | Idempotent upgrade from the gateway-fields baseline through DisableETipaxLtlService |
| `api/` and `api.zip` | Framework-dependent Release publish for the API |
| `manifest.json` | Source baseline, migration range, validation results and artifact hashes |

The package excludes `appsettings.Development.json` and the default `IdentityCertificate.pfx`.
Supply the production certificate and secrets through the server configuration.
Retain the existing valid production certificate and database-backed Data Protection keys.
The server needs the ASP.NET Core 10 runtime. This package contains the API, not the separate React frontend.

## Database prerequisites

- Back up the target database and verify its restore procedure.
- Check `dbo.__EFMigrationsHistory` on the actual target database.
- Require baseline `20261027135810_AddGatewayFieldsToOrderPayment` before using this upgrade script.
- Require database compatibility level 130 or newer for `OPENJSON`.
- Use a migration account with permission to alter schema and update reference data.
- Stop API instances and workers during the migration window.

`preflight.sql` is prepended to the artifact. It checks the baseline, compatibility level and newer migration history.
The script sets SQL session options required by filtered indexes.
Its migration-history guards skip migrations that were already applied.
It is not a fresh-install script. Earlier audit migrations need separate review on older databases.

Example after explicitly selecting the server and database:

```powershell
sqlcmd -S <server> -d <database> -E -b -f 65001 -i shipping-upgrade.sql
```

Choose the deployment environment's authentication and certificate-trust settings. Do not copy local connection settings to production.
The artifact uses UTF-8 with a BOM. Keep UTF-8 input enabled to preserve Persian seed titles.

The upgrade seeds the admin role, shipping tables, cities, service definitions and status mappings.
The latest migration disables LTL; five service definitions remain active.
Packaging and content catalogues require the authenticated reference synchronization after deployment.

## Required configuration

| Key | Check |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` |
| `ConnectionStrings__SqlServerConnectionString` | Point to the approved production database |
| `AppSettings__IdentitySettings__IdentityCertificateBase64` | Supply the production PFX, or provision its file on the server |
| `AppSettings__IdentitySettings__IdentityCertificatePassword` | Match the production PFX |
| `Providers__ETipax__BaseUrl` | Confirm the production account endpoint; the default is a test endpoint |
| `Providers__ETipax__Username`, `Password`, `ApiKey` | Configure the corresponding provider secret keys from setup.md |
| `Providers__ETipax__TrackingBaseUrl`, `TrackingToken` | Configure production Jet tracking separately |
| `Providers__ETipax__WebhookSecret` | Agree the secret with the carrier and register the public webhook URL |
| `Cors__AllowedOrigins__0` | Set the actual frontend origin |
| `Payment__AllowedCallbackOrigins__0` | Set the approved payment return origin |
| `ForwardedHeaders__KnownProxies__0` | Set the actual reverse proxy when applicable |
| SMS settings | Configure the production SMS account; Development OTP is not active in Production |

Keep `ReferenceSyncEnabled` and `ReconcileEventsEnabled` false until migrations and credentials are ready.
Perform the initial admin-only `POST /api/Shipment/SynchronizeReferences`, then enable the required jobs.

## Deployment order

1. Commit the reviewed fixes, tag the release, and regenerate the artifacts from that source revision.
2. Confirm runtime, certificate, secrets, proxy settings, database history and backup.
3. Stop API instances and run the upgrade SQL with failure-on-error enabled.
4. Deploy the API files and apply the Production configuration.
5. Start one instance and verify startup logs and migration history.
6. Synchronize references and verify city, service, packaging and content catalogues.
7. Verify login, order listing, draft editing and pricing with the compatible frontend.
8. Enable jobs and verify webhook authentication and event processing.

Carrier registration, cancellation and payment require a separately authorized smoke test.
Schema and data backfills do not have complete automatic reversal. Use the backup recovery plan for a failed release.

## Review findings

SQL Server compiled new-column seed statements too early in the generated idempotent script.
The affected seed and payment-backfill statements now run through `MigrationSql.ExecuteDeferred`.
Existing migration identifiers and resulting schema are unchanged; already-applied migrations are not rerun.
`MigrationUpgradeTests` checks an upgrade from the baseline and a second execution of the same generated SQL.
The final artifact was also executed twice with `sqlcmd` on an isolated database.

API compilation and publish succeeded. Tests use fake external providers and isolated SQL databases.
Validation: 82 unit tests and 172 integration tests passed; one existing placeholder remains skipped.
The published API also started in Production mode with a temporary audit certificate and isolated database.
`GET /api/Common/GetOrderStatuses` returned HTTP 200 with 13 statuses; the audit host was stopped afterwards.
This startup probe did not use the real production certificate or call external carrier/payment services.
The NuGet audit reported no known vulnerable server packages, including transitive dependencies.
The broader solution build could not complete restore for the separate legacy client projects during this review.
This does not replace a build and release review for those clients.

## Open release constraints

- SEP verification remains unimplemented in `PaymentService.VerifyWithGatewayAsync`. Online payment is not release-ready.
- Previously observed SEP stage credentials returned 401 for payment-token creation; production credentials need verification.
- Tipax and Jet default hosts are test hosts. A production endpoint and account must be confirmed externally.
- Logout remains client-only. An issued bearer token remains valid until expiry; the accepted risk is recorded in tasks.md.
- The production schema, existing reference data, certificate, routing and runtime have not been verified here.
- An incompatible frontend can still send stale content IDs; deploy the frontend contract changes with this API version.

This preparation does not deploy, apply migrations to the business database, register shipments, or create payments.
