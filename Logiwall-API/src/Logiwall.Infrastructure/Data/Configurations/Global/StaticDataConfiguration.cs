using Logiwall.Domain.Entities.Global;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.Global;
public class StaticDataConfiguration : IEntityTypeConfiguration<StaticData>
{
    public void Configure(EntityTypeBuilder<StaticData> builder)
    {
        
    }
}
