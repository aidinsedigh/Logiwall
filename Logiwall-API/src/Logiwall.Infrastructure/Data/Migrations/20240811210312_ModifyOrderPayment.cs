using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentGateway",
                table: "OrderPayments");

            migrationBuilder.RenameColumn(
                name: "TrackingCode",
                table: "OrderPayments",
                newName: "PayTrackingCode");

            migrationBuilder.RenameColumn(
                name: "PaymentAmount",
                table: "OrderPayments",
                newName: "Amount");

            migrationBuilder.AddColumn<int>(
                name: "MerchantId",
                table: "OrderPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayDate",
                table: "OrderPayments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayTime",
                table: "OrderPayments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TerminalId",
                table: "OrderPayments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "PayDate",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "PayTime",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "TerminalId",
                table: "OrderPayments");

            migrationBuilder.RenameColumn(
                name: "PayTrackingCode",
                table: "OrderPayments",
                newName: "TrackingCode");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "OrderPayments",
                newName: "PaymentAmount");

            migrationBuilder.AddColumn<int>(
                name: "PaymentGateway",
                table: "OrderPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
