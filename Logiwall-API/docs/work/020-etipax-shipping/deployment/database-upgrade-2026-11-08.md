# Database upgrade execution: 2026-11-08

The user authorized the upgrade of the designated Liara SQL Server database.
The database password was not written to repository files or this report.

## Before execution

- SQL Server version: 16.0.4105.2; database compatibility level: 160.
- Last applied migration: `20261028090000_SeedAdminRole`.
- Counts: zero orders, zero parcels, four users.
- No other active request was observed during preflight.
- Created a COPY_ONLY backup with CHECKSUM and validated it with RESTORE VERIFYONLY.
- Backup remains on the database server at:
  `/var/opt/mssql/data/myDB_pre_shipping_20261108_5990f5ce62884c7da188376dbd5db87c.bak`.
  This is a server-local backup; no off-host copy was made.

## Applied changes

Executed the hash-verified `artifacts/releases/2026-11-08-0e16b72/shipping-upgrade.sql`.
All pending work ran inside one outer transaction. Checks passed before commit.

1. `20261103201830_ETipaxShipping`
2. `20261103202834_ShippingEventReconciliationSchedule`
3. `20261105134821_CarrierServicesAndSharedPayment`
4. `20261106115953_UnifiedOrderStatuses`
5. `20261106130903_SimplifyPublicOrderStatuses`
6. `20261107083207_SelectableETipaxServices`
7. `20261107091650_DisableETipaxLtlService`

## Verification after commit

- Migration history reaches `20261107091650_DisableETipaxLtlService`.
- All six shipping tables exist.
- Shipping-related foreign keys are enabled and trusted.
- Tipax has five active service references and thirteen mapped status references.
- Tipax city mappings contain 678 origin and 678 destination cities.
- The provider's Persian title is correct.
- Counts remain zero orders, zero parcels, four users.

## Remaining server work

Initially packaging and content references contained zero rows.
On 2026-11-08 the user authorized reuse of the existing local Tipax account on Liara.
Configured its BaseUrl, Username, Password and ApiKey through environment variables without storing secrets in the repository.
The account uses the same test endpoint as local development: `https://omtestapi.tipax.ir`.
Enabled reference synchronization every 1440 minutes. The server worker completed the first synchronization.
Verified active references: 51 packaging, 35 content, 13 status and 5 service records.
The next synchronization lease is scheduled for 2026-11-09 10:00:32 UTC.
Frontend clients must reload server reference IDs; local database IDs were not copied.

The initial database migration did not deploy API binaries, frontend, certificates or provider secrets.
Subsequent fixed-OTP deployment and carrier configuration are recorded in tasks.md.
No carrier registration, cancellation, pricing request or payment was initiated.
The release constraints in [release-readiness.md](release-readiness.md) still apply.
