using Microsoft.EntityFrameworkCore.Migrations;

namespace Logiwall.Infrastructure.Data.Migrations;

public partial class CarrierServicesAndSharedPayment
{
    private static void SeedServices(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @provider smallint=(SELECT Id FROM ServiceProviders WHERE Code='etipax');
        IF @provider IS NULL THROW 50001, 'ETipax provider is missing.', 1;
        DECLARE @services TABLE(Id int,Title nvarchar(200));
        -- Source: ShopLink SeedStaticTipaxReferences (20260923000100), not a remote catalogue endpoint.
        INSERT @services VALUES
        (1,N'ارسال زمینی همان روز'),(11,N'نکست دی'),(2,N'ارسال زمینی یک روزه'),
        (3,N'ارسال زمینی دو روزه'),(4,N'ارسال زمینی سه روزه'),(5,N'اکسپرس ویژه بین شهری'),
        (10,N'LTL'),(7,N'اکسپرس درون شهری'),(14,N'سرویس لجستیک معکوس'),
        (15,N'سرویس اکسپرس هوایی'),(12,N'سرویس همان روز'),(13,N'سرویس اکسپرس پلاس'),(6,N'بین الملل');
        INSERT ProviderReferenceItems(ServiceProviderId,Category,ExternalCode,Title,MetadataJson,IsActive,SyncedAt)
        SELECT @provider,'service',CONVERT(nvarchar(100),s.Id),s.Title,
            CONCAT(N'{"id":',s.Id,N',"source":"shoplink-static-v1"}'),1,SYSDATETIMEOFFSET()
        FROM @services s WHERE NOT EXISTS(SELECT 1 FROM ProviderReferenceItems r
            WHERE r.ServiceProviderId=@provider AND r.Category='service' AND r.ExternalCode=CONVERT(nvarchar(100),s.Id));
        """);
}
