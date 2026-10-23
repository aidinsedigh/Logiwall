using Logiwall.Domain.Entities.Global;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.Global;
internal class StaticDataMapperConfiguration : IEntityTypeConfiguration<StaticDataMapper>
{
    public void Configure(EntityTypeBuilder<StaticDataMapper> builder)
    {
        builder.HasOne(t => t.StaticData).WithMany().HasForeignKey(t => t.StaticDataId);
    }
}
