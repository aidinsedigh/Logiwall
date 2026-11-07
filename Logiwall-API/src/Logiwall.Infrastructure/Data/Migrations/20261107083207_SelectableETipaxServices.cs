using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectableETipaxServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only these eTipax services are offered; the other codes are aliases of them.
            migrationBuilder.Sql("""
                DECLARE @provider smallint=(SELECT Id FROM ServiceProviders WHERE Code='etipax');
                DECLARE @selectable TABLE(Id int,Title nvarchar(200));
                INSERT @selectable VALUES
                (4,N'اکسپرس'),(10,N'LTL'),(12,N'همان روز'),(13,N'اکسپرس پلاس'),(14,N'لجستیک معکوس'),(15,N'اکسپرس هوایی');
                UPDATE r SET Title=s.Title, IsActive=1, SyncedAt=SYSDATETIMEOFFSET()
                FROM ProviderReferenceItems r JOIN @selectable s ON r.ExternalCode=CONVERT(nvarchar(100),s.Id)
                WHERE r.ServiceProviderId=@provider AND r.Category='service';
                UPDATE ProviderReferenceItems SET IsActive=0, SyncedAt=SYSDATETIMEOFFSET()
                WHERE ServiceProviderId=@provider AND Category='service' AND IsActive=1
                    AND ExternalCode NOT IN (SELECT CONVERT(nvarchar(100),Id) FROM @selectable);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-enabling the aliases would show duplicate service choices again.
        }
    }
}
