using Microsoft.EntityFrameworkCore.Migrations;

namespace Logiwall.Infrastructure.Data.Migrations;

public partial class ETipaxShipping
{
    private static void SeedCarrier(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE ServiceProviders SET IsActive=1 WHERE Name<>'MyTipaxProvider';
            UPDATE ServiceProviders SET Code='etipax' WHERE Name='ETipaxCarrier' AND Code IS NULL;
            IF NOT EXISTS(SELECT 1 FROM ServiceProviders WHERE Code='etipax')
                INSERT ServiceProviders(Name,Title,Code,IsActive) VALUES('ETipaxCarrier',N'تیپاکس','etipax',1);
            INSERT ShippingJobLeases(Name,Owner,ExpiresAt) VALUES
                ('shipping-reference-sync','00000000-0000-0000-0000-000000000000','2000-01-01'),
                ('shipping-event-reconciliation','00000000-0000-0000-0000-000000000000','2000-01-01');
            """);
        using var stream = typeof(ETipaxShipping).Assembly.GetManifestResourceStream(
            "Logiwall.Infrastructure.Data.Seed.ETipaxCities.v1.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd().TrimStart('\uFEFF').Replace("'", "''");
        migrationBuilder.Sql($"""
            DECLARE @cities TABLE(Code nvarchar(20),Title nvarchar(100),Province nvarchar(100));
            INSERT @cities SELECT CONVERT(nvarchar(20),id),title,stateTitle
            FROM OPENJSON(N'{json}') WITH(id int,title nvarchar(100),stateTitle nvarchar(100));
            DECLARE @code nvarchar(20), @title nvarchar(100), @province nvarchar(100), @type int, @id int;
            DECLARE cities CURSOR LOCAL FAST_FORWARD FOR
                SELECT Code,Title,Province,t.Type FROM @cities CROSS JOIN (VALUES(11),(12)) t(Type);
            OPEN cities;
            FETCH NEXT FROM cities INTO @code,@title,@province,@type;
            WHILE @@FETCH_STATUS=0
            BEGIN
                SET @id=NULL;
                -- Reuse only an unambiguous city. Old carrier codes are never reinterpreted.
                IF (SELECT COUNT(*) FROM StaticDatas WHERE Type=@type AND Title=@title AND (ProvinceTitle IS NULL OR ProvinceTitle=@province))=1
                    SELECT @id=Id FROM StaticDatas WHERE Type=@type AND Title=@title AND (ProvinceTitle IS NULL OR ProvinceTitle=@province);
                IF @id IS NULL
                BEGIN
                    INSERT StaticDatas(Title,Type,ProvinceTitle) VALUES(@title,@type,@province);
                    SET @id=SCOPE_IDENTITY();
                END
                UPDATE StaticDatas SET ProvinceTitle=@province WHERE Id=@id;
                IF NOT EXISTS(SELECT 1 FROM StaticDataMappers WHERE StaticDataId=@id AND ProviderType=1)
                    INSERT StaticDataMappers(Code,ProviderType,StaticDataId) VALUES(@code,1,@id);
                ELSE IF NOT EXISTS(SELECT 1 FROM StaticDataMappers WHERE StaticDataId=@id AND ProviderType=1 AND Code=@code)
                BEGIN
                    INSERT StaticDatas(Title,Type,ProvinceTitle) VALUES(@title,@type,@province);
                    SET @id=SCOPE_IDENTITY();
                    INSERT StaticDataMappers(Code,ProviderType,StaticDataId) VALUES(@code,1,@id);
                END
                FETCH NEXT FROM cities INTO @code,@title,@province,@type;
            END
            CLOSE cities; DEALLOCATE cities;
            """);
    }
}
