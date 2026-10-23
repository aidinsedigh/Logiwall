using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOrderPaymentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Orders",
                newName: "State");

            migrationBuilder.AddColumn<int>(
                name: "PaymentGateway",
                table: "OrderPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "OrderPayments",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentGateway",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "OrderPayments");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Orders",
                newName: "Status");
        }
    }
}
