using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesAndOtherConstants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM [dbo].[ServiceProviders] WHERE Name = 'MyTipaxProvider')
                    INSERT INTO [dbo].[ServiceProviders]([Name],[Title])VALUES('MyTipaxProvider',N'تیپاکس')

                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxBaseUrl')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxBaseUrl','https://mytipaxtest.tipax.ir:8081')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxUsernameToken')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxUsernameToken','jet')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxPasswordToken')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxPasswordToken','jet1403')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxPasswordLogin')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxPasswordLogin','logiwall1403')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxUsernameLogin')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxUsernameLogin','09171408429')
                IF NOT EXISTS (SELECT * FROM [dbo].[AppSettings] WHERE KeyName = 'MyTipaxInquiryVersion')
	                INSERT INTO [dbo].[AppSettings] (KeyName,Value) VALUES ('MyTipaxInquiryVersion','w.1.14.4')
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
