-- Run in the explicitly selected deployment database, after taking a backup.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 130
    THROW 51000, 'Database compatibility level must be at least 130 for OPENJSON.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    THROW 51001, 'Migration history is missing. This is an upgrade script, not a fresh installation.', 1;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261027135810_AddGatewayFieldsToOrderPayment')
    THROW 51002, 'Required migration baseline is missing. Review the earlier upgrade first.', 1;
IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId > N'20261107091650_DisableETipaxLtlService')
    THROW 51003, 'Database has migrations newer than this release. Check the release version.', 1;
GO
