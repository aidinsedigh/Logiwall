using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClearSharedDefaultPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users created by the OTP flow before plan 001 share one hardcoded password.
            // Users without a People row never ran Person/Register, so they still have it. Clear it.
            migrationBuilder.Sql(@"
                UPDATE [dbo].[Users]
                SET [PasswordHash] = NULL,
                    [SecurityStamp] = CONVERT(nvarchar(36), NEWID())
                WHERE [PasswordHash] IS NOT NULL
                  AND [Id] NOT IN (SELECT [UserId] FROM [dbo].[People]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design: the old shared password must not be restored.
        }
    }
}
