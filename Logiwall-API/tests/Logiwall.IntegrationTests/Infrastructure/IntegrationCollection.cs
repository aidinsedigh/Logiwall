namespace Logiwall.IntegrationTests.Infrastructure;

[CollectionDefinition("api")]
public class IntegrationCollection : ICollectionFixture<SqlServerFixture>
{
}
