using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ETipaxShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProvinceTitle",
                table: "StaticDatas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ServiceProviders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ServiceProviders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalStatusCode",
                table: "OrderStatuses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShipmentEventId",
                table: "OrderStatuses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShipmentParcelId",
                table: "OrderStatuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentReferenceId",
                table: "OrderParcels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackagingReferenceId",
                table: "OrderParcels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarrierRequestJson",
                table: "OrderInqueryOptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "OrderInqueryOptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalServiceId",
                table: "OrderInqueryOptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderReferenceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceProviderId = table.Column<short>(type: "smallint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExternalCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderReferenceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderReferenceItems_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<short>(type: "smallint", nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    TraceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingJobLeases",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Owner = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingJobLeases", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAttempts_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentParcels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    OrderParcelId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TrackingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalStatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastExternalEventId = table.Column<long>(type: "bigint", nullable: true),
                    LastStatusAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentParcels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentParcels_OrderParcels_OrderParcelId",
                        column: x => x.OrderParcelId,
                        principalTable: "OrderParcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentParcels_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceProviderId = table.Column<short>(type: "smallint", nullable: false),
                    ExternalEventId = table.Column<long>(type: "bigint", nullable: false),
                    TrackingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatusTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShipmentParcelId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Applied = table.Column<bool>(type: "bit", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentEvents_ShipmentParcels_ShipmentParcelId",
                        column: x => x.ShipmentParcelId,
                        principalTable: "ShipmentParcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_Code",
                table: "ServiceProviders",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_ShipmentEventId",
                table: "OrderStatuses",
                column: "ShipmentEventId",
                unique: true,
                filter: "[ShipmentEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatuses_ShipmentParcelId",
                table: "OrderStatuses",
                column: "ShipmentParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderParcels_ContentReferenceId",
                table: "OrderParcels",
                column: "ContentReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderParcels_PackagingReferenceId",
                table: "OrderParcels",
                column: "PackagingReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderReferenceItems_ServiceProviderId_Category_ExternalCode",
                table: "ProviderReferenceItems",
                columns: new[] { "ServiceProviderId", "Category", "ExternalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAttempts_ShipmentId",
                table: "ShipmentAttempts",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentEvents_Processed_ReceivedAt",
                table: "ShipmentEvents",
                columns: new[] { "Processed", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentEvents_ServiceProviderId_ExternalEventId",
                table: "ShipmentEvents",
                columns: new[] { "ServiceProviderId", "ExternalEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentEvents_ShipmentParcelId",
                table: "ShipmentEvents",
                column: "ShipmentParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentParcels_OrderParcelId",
                table: "ShipmentParcels",
                column: "OrderParcelId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentParcels_ShipmentId_Position",
                table: "ShipmentParcels",
                columns: new[] { "ShipmentId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentParcels_TrackingCode",
                table: "ShipmentParcels",
                column: "TrackingCode");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ServiceProviderId",
                table: "Shipments",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TraceCode",
                table: "Shipments",
                column: "TraceCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderParcels_ProviderReferenceItems_ContentReferenceId",
                table: "OrderParcels",
                column: "ContentReferenceId",
                principalTable: "ProviderReferenceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderParcels_ProviderReferenceItems_PackagingReferenceId",
                table: "OrderParcels",
                column: "PackagingReferenceId",
                principalTable: "ProviderReferenceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatuses_ShipmentParcels_ShipmentParcelId",
                table: "OrderStatuses",
                column: "ShipmentParcelId",
                principalTable: "ShipmentParcels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            SeedCarrier(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderParcels_ProviderReferenceItems_ContentReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderParcels_ProviderReferenceItems_PackagingReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatuses_ShipmentParcels_ShipmentParcelId",
                table: "OrderStatuses");

            migrationBuilder.DropTable(
                name: "ProviderReferenceItems");

            migrationBuilder.DropTable(
                name: "ShipmentAttempts");

            migrationBuilder.DropTable(
                name: "ShipmentEvents");

            migrationBuilder.DropTable(
                name: "ShippingJobLeases");

            migrationBuilder.DropTable(
                name: "ShipmentParcels");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_Code",
                table: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_ShipmentEventId",
                table: "OrderStatuses");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatuses_ShipmentParcelId",
                table: "OrderStatuses");

            migrationBuilder.DropIndex(
                name: "IX_OrderParcels_ContentReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropIndex(
                name: "IX_OrderParcels_PackagingReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropColumn(
                name: "ProvinceTitle",
                table: "StaticDatas");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "ExternalStatusCode",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "ShipmentEventId",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "ShipmentParcelId",
                table: "OrderStatuses");

            migrationBuilder.DropColumn(
                name: "ContentReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropColumn(
                name: "PackagingReferenceId",
                table: "OrderParcels");

            migrationBuilder.DropColumn(
                name: "CarrierRequestJson",
                table: "OrderInqueryOptions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "OrderInqueryOptions");

            migrationBuilder.DropColumn(
                name: "ExternalServiceId",
                table: "OrderInqueryOptions");
        }
    }
}
