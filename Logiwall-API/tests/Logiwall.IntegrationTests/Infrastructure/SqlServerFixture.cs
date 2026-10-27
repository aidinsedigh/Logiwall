using Logiwall.Server.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>One SQL Server container and one migrated database for the whole "api" collection.</summary>
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public LogiwallApiFactory Factory { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = "LogiwallTests"
        }.ConnectionString;

        Factory = new LogiwallApiFactory(ConnectionString);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
        await container.DisposeAsync();
    }

    /// <summary>Runs <paramref name="action"/> with a fresh scoped <see cref="AppDbContext"/>.</summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public ApiClient CreateApiClient() =>
        new(Factory, Factory.Services.GetRequiredService<FakeSmsService>());
}
