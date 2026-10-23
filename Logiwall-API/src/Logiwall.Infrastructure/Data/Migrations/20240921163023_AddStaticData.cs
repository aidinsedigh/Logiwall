using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaticData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DelivereDate",
                table: "OrderInqueryOptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "OrderInqueryOptions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StaticDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MyProperty = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticDatas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaticDataMappers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProviderType = table.Column<short>(type: "smallint", nullable: false),
                    StaticDataId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticDataMappers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaticDataMappers_StaticDatas_StaticDataId",
                        column: x => x.StaticDataId,
                        principalTable: "StaticDatas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaticDataMappers_StaticDataId",
                table: "StaticDataMappers",
                column: "StaticDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaticDataMappers");

            migrationBuilder.DropTable(
                name: "StaticDatas");

            migrationBuilder.DropColumn(
                name: "DelivereDate",
                table: "OrderInqueryOptions");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "OrderInqueryOptions");
        }
    }
}
