using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSepInAppSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectUrl')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectUrl', 'https://idn.seppay.ir/connect/')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectUsername')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectUsername', '')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectPassword')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectPassword', '')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectScope')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectScope', '724TP')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectAuthorization')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectAuthorization', '')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepPayBaseUrl')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepPayBaseUrl', 'https://api-stage.724.ir/api/webview/v1/Payment/')
                IF NOT EXISTS (SELECT * FROM AppSettings WHERE KeyName = 'SepConnectClient')
	                INSERT INTO AppSettings(KeyName, Value) VALUES ('SepConnectClient', '')
                
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
