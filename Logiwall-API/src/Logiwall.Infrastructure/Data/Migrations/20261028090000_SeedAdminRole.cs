using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Blazor admin panel and its endpoints need the Admin role (policy AdminsOnly, ADR-0005).
            // The existing database can already have it, so insert it only when it is missing.
            // An operator gives a user the role with the SQL in ADR-0005.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [NormalizedName] = N'ADMIN')
                    INSERT INTO [dbo].[Roles] ([Name], [NormalizedName], [ConcurrencyStamp])
                    VALUES (N'Admin', N'ADMIN', CONVERT(nvarchar(36), NEWID()));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the role: users can have it, and their admin access must not go away silently.
        }
    }
}
