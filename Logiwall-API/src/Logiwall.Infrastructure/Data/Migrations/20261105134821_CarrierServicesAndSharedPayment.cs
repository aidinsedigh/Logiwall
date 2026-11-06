using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CarrierServicesAndSharedPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "OrderInqueryOptions",
                type: "int",
                nullable: false,
                defaultValue: 71);
            SeedServices(migrationBuilder);
            migrationBuilder.Sql("UPDATE OrderInqueryOptions SET PaymentMethod=72 WHERE ISJSON(CarrierRequestJson)=1 AND JSON_VALUE(CarrierRequestJson, '$.packages[0].paymentType')='20';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "OrderInqueryOptions");
        }
    }
}
