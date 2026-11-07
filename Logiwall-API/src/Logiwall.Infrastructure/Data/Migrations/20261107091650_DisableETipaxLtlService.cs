using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logiwall.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DisableETipaxLtlService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // eTipax LTL (10) rejects every pack type for our account; offer it again when eTipax enables it.
            migrationBuilder.Sql("""
                UPDATE r SET IsActive=0, SyncedAt=SYSDATETIMEOFFSET()
                FROM ProviderReferenceItems r JOIN ServiceProviders p ON p.Id=r.ServiceProviderId
                WHERE p.Code='etipax' AND r.Category='service' AND r.ExternalCode='10';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE r SET IsActive=1, SyncedAt=SYSDATETIMEOFFSET()
                FROM ProviderReferenceItems r JOIN ServiceProviders p ON p.Id=r.ServiceProviderId
                WHERE p.Code='etipax' AND r.Category='service' AND r.ExternalCode='10';
                """);
        }
    }
}
