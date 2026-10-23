using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountInfoInAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'LogiwallShare')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('LogiwallShare','60')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'LogiwallIban')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('LogiwallIban','IR470210000001000209682201')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxShare')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxShare','40')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxIban')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxIban','IR820150000003101077878516')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
