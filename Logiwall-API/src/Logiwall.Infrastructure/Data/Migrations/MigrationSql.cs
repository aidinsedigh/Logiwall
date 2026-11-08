using Microsoft.EntityFrameworkCore.Migrations;

namespace Logiwall.Infrastructure.Data.Migrations;

internal static class MigrationSql
{
    // Defer compilation until preceding column additions have run. SQL Server otherwise
    // compiles references to new columns even inside EF's idempotent IF guards.
    public static void ExecuteDeferred(MigrationBuilder builder, string sql)
        => builder.Sql("EXEC(N'" + sql.Replace("'", "''", StringComparison.Ordinal) + "');");
}
