using Logiwall.Domain.Entities.Global;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.Global;
public class AppSettingLogiwallConfiguration : IEntityTypeConfiguration<AppSettingLogiwall>
{
    public void Configure(EntityTypeBuilder<AppSettingLogiwall> builder)
    {
        builder.ToTable("AppSettings");
        builder.HasIndex(t => t.KeyName).IsUnique();
    }
}
