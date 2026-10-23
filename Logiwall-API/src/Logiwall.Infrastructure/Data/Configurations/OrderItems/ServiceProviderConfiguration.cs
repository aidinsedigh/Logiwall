using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class ServiceProviderConfiguration : IEntityTypeConfiguration<Domain.Entities.OrderItems.ServiceProvider>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.OrderItems.ServiceProvider> builder)
    {
        builder.HasIndex(t => new { t.Name })
            .IsUnique();
    }
}
