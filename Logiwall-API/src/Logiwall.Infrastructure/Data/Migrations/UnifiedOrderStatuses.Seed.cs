using Microsoft.EntityFrameworkCore.Migrations;

namespace Logiwall.Infrastructure.Data.Migrations;

public partial class UnifiedOrderStatuses
{
    private static void SeedAndBackfill(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @provider smallint = (SELECT Id FROM ServiceProviders WHERE Code='etipax');
        DECLARE @map TABLE(Code nvarchar(100),Title nvarchar(200),Mapped smallint);
        INSERT INTO @map VALUES
        ('30',N'در دست جمع آوری',12),('34',N'ابطال شد',22),('38',N'جمع آوری شد',13),
        ('50',N'تحویل به گیرنده',20),('51',N'عودت شد',18),('52',N'برگشت خورد',17),
        ('57',N'لغو شد',21),('58',N'در حال پردازش',15),('59',N'ثبت اولیه',10),
        ('60',N'رسیدن به شهر مقصد',16),('61',N'در انتظار تایید',11),('62',N'در مسیر مبدا',14),
        ('63',N'مختومه عودتی',19);
        UPDATE r SET MappedOrderStatus=m.Mapped FROM ProviderReferenceItems r
        JOIN @map m ON r.ExternalCode=m.Code WHERE r.ServiceProviderId=@provider AND r.Category='status';
        INSERT INTO ProviderReferenceItems(ServiceProviderId,Category,ExternalCode,Title,MetadataJson,IsActive,SyncedAt,MappedOrderStatus)
        SELECT @provider,'status',m.Code,m.Title,N'{}',1,SYSUTCDATETIME(),m.Mapped FROM @map m
        WHERE @provider IS NOT NULL AND NOT EXISTS(SELECT 1 FROM ProviderReferenceItems r
            WHERE r.ServiceProviderId=@provider AND r.Category='status' AND r.ExternalCode=m.Code);

        -- Successful mutation status codes are confirmed by the ShopLink implementation.
        UPDATE p SET ExternalStatusCode=CASE WHEN s.State=5 THEN '57' ELSE '59' END
        FROM ShipmentParcels p JOIN Shipments s ON s.Id=p.ShipmentId
        WHERE s.ServiceProviderId=@provider AND s.State IN(2,5) AND p.ExternalStatusCode IS NULL;
        UPDATE p SET MappedOrderStatus=r.MappedOrderStatus,StatusTitle=COALESCE(p.StatusTitle,r.Title)
        FROM ShipmentParcels p JOIN Shipments s ON s.Id=p.ShipmentId JOIN ProviderReferenceItems r
        ON r.ServiceProviderId=s.ServiceProviderId AND r.Category='status' AND r.ExternalCode=p.ExternalStatusCode;
        UPDATE h SET MappedOrderStatus=r.MappedOrderStatus FROM OrderStatuses h JOIN ProviderReferenceItems r
        ON r.ServiceProviderId=h.ServiceProviderId AND r.Category='status' AND r.ExternalCode=h.ExternalStatusCode;

        UPDATE o SET OverallStatus=CASE WHEN EXISTS(SELECT 1 FROM OrderInqueryOptions q WHERE q.OrderId=o.Id AND q.Choosen=1) THEN 1 ELSE 0 END FROM Orders o;
        UPDATE o SET OverallStatus=CASE s.State WHEN 1 THEN 2 WHEN 2 THEN 10 WHEN 3 THEN 3 WHEN 4 THEN 4 WHEN 5 THEN 21 ELSE o.OverallStatus END
        FROM Orders o JOIN Shipments s ON s.OrderId=o.Id;
        UPDATE o SET OverallStatus=a.Mapped FROM Orders o JOIN Shipments s ON s.OrderId=o.Id
        CROSS APPLY(SELECT CASE
          WHEN COUNT(*)<>COUNT(p.MappedOrderStatus) THEN 4
          WHEN MIN(p.MappedOrderStatus)=MAX(p.MappedOrderStatus) THEN MIN(p.MappedOrderStatus)
          WHEN SUM(CASE WHEN p.MappedOrderStatus=20 THEN 1 ELSE 0 END)>0 THEN 23
          WHEN MAX(p.MappedOrderStatus)>=17 THEN 24
          WHEN MIN(p.MappedOrderStatus)=13 AND SUM(CASE WHEN p.MappedOrderStatus=14 THEN 1 ELSE 0 END)>0 THEN 14
          ELSE MIN(p.MappedOrderStatus) END Mapped,COUNT(*) Cnt
          FROM ShipmentParcels p WHERE p.ShipmentId=s.Id) a
        WHERE s.State=2 AND a.Cnt>0;
        """);
}
