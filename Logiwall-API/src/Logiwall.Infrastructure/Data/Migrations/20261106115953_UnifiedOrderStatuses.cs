using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedOrderStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "MappedOrderStatus",
                table: "ShipmentParcels",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "MappedOrderStatus",
                table: "ProviderReferenceItems",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "MappedOrderStatus",
                table: "OrderStatuses",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "OverallStatus",
                table: "Orders",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
            SeedAndBackfill(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MappedOrderStatus",
                table: "ShipmentParcels");

            migrationBuilder.DropColumn(
                name: "MappedOrderStatus",
                table: "ProviderReferenceItems");

            migrationBuilder.DropColumn(
                name: "MappedOrderStatus",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "OverallStatus",
                table: "Orders");
        }
    }
}

