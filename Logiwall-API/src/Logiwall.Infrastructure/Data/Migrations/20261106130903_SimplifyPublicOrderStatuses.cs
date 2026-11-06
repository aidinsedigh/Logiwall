using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPublicOrderStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE ProviderReferenceItems SET MappedOrderStatus=CASE WHEN MappedOrderStatus=11 THEN 10 ELSE 0 END
                WHERE MappedOrderStatus IN(1,2,3,4,11);
                UPDATE ShipmentParcels SET MappedOrderStatus=CASE WHEN MappedOrderStatus=11 THEN 10 ELSE 0 END
                WHERE MappedOrderStatus IN(1,2,3,4,11);
                UPDATE OrderStatuses SET MappedOrderStatus=CASE WHEN MappedOrderStatus=11 THEN 10 ELSE 0 END,
                    Status=CASE WHEN MappedOrderStatus=11 THEN 10 ELSE 0 END WHERE MappedOrderStatus IN(1,2,3,4,11);
                UPDATE Orders SET OverallStatus=CASE WHEN OverallStatus IN(11,23,24) THEN 10 ELSE 0 END
                WHERE OverallStatus IN(1,2,3,4,11,23,24);
                UPDATE o SET OverallStatus=10 FROM Orders o JOIN Shipments s ON s.OrderId=o.Id
                WHERE s.RegisteredAt IS NOT NULL AND o.OverallStatus=0;
                UPDATE o SET OverallStatus=CASE
                    WHEN a.Cnt<>a.MappedCount THEN o.OverallStatus
                    WHEN a.Outstanding IS NULL THEN 20
                    WHEN a.Outstanding=13 AND a.HasOriginTransit>0 THEN 14 ELSE a.Outstanding END
                FROM Orders o JOIN Shipments s ON s.OrderId=o.Id
                CROSS APPLY(SELECT COUNT(*) Cnt,COUNT(p.MappedOrderStatus) MappedCount,
                    MIN(CASE WHEN p.MappedOrderStatus<>20 THEN p.MappedOrderStatus END) Outstanding,
                    SUM(CASE WHEN p.MappedOrderStatus=14 THEN 1 ELSE 0 END) HasOriginTransit
                    FROM ShipmentParcels p WHERE p.ShipmentId=s.Id) a
                WHERE s.RegisteredAt IS NOT NULL AND a.Cnt>0;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
