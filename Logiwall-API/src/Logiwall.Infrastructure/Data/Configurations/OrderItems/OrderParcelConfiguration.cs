using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class OrderParcelConfiguration : IEntityTypeConfiguration<OrderParcel>
{
    public void Configure(EntityTypeBuilder<OrderParcel> builder)
    {
        builder.HasOne<Logiwall.Domain.Entities.OrderItems.ProviderReferenceItem>().WithMany()
            .HasForeignKey(x => x.PackagingReferenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Logiwall.Domain.Entities.OrderItems.ProviderReferenceItem>().WithMany()
            .HasForeignKey(x => x.ContentReferenceId).OnDelete(DeleteBehavior.Restrict);
    }
}
