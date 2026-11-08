using System.Text.RegularExpressions;
using Logiwall.Infrastructure.Data;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Logiwall.IntegrationTests.Migrations;

[Collection("api")]
public class MigrationUpgradeTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Shipping_idempotent_script_upgrades_the_baseline_and_can_run_twice()
    {
        const string baseline = "20261027135810_AddGatewayFieldsToOrderPayment";
        const string latest = "20261107091650_DisableETipaxLtlService";
        var connectionString = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = "LogiwallMigrationTests_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(baseline);
            var script = migrator.GenerateScript(baseline, latest, MigrationsSqlGenerationOptions.Idempotent);
            var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            for (var run = 0; run < 2; run++)
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
                {
                    await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                    await command.ExecuteNonQueryAsync();
                }
            }
            Assert.Equal(latest, (await db.Database.GetAppliedMigrationsAsync()).Last());
            Assert.Equal(5, await db.ProviderReferenceItems.CountAsync(r => r.Category == "service" && r.IsActive));
            Assert.Equal(13, await db.ProviderReferenceItems.CountAsync(r => r.Category == "status" && r.MappedOrderStatus != null));
            Assert.Equal("تیپاکس", await db.ServiceProviders.Where(p => p.Code == "etipax").Select(p => p.Title).SingleAsync());
            Assert.Equal("اکسپرس", await db.ProviderReferenceItems.Where(r => r.Category == "service" && r.ExternalCode == "4").Select(r => r.Title).SingleAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
