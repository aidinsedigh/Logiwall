using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGatewayFieldsToOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GatewayReferenceCode",
                table: "OrderPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GatewayRequestId",
                table: "OrderPayments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VerifiedAmount",
                table: "OrderPayments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                table: "OrderPayments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_GatewayReferenceCode",
                table: "OrderPayments",
                column: "GatewayReferenceCode",
                unique: true,
                filter: "[GatewayReferenceCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_GatewayReferenceCode",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "GatewayReferenceCode",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "GatewayRequestId",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "VerifiedAmount",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "OrderPayments");
        }
    }
}
