using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProviderSecretsFromAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [dbo].[AppSettings]
                WHERE [KeyName] IN ('SepConnectUsername','SepConnectPassword','SepConnectAuthorization','SepConnectClient',
                                    'MyTipaxUsernameToken','MyTipaxPasswordToken','MyTipaxUsernameLogin','MyTipaxPasswordLogin');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible: credentials now come from configuration.
        }
    }
}
