using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.Property(x => x.ExternalStatusCode).HasMaxLength(50);
        builder.HasIndex(x => x.ShipmentEventId).IsUnique().HasFilter("[ShipmentEventId] IS NOT NULL");
        builder.HasOne<Logiwall.Domain.Entities.OrderItems.ShipmentParcel>().WithMany()
            .HasForeignKey(x => x.ShipmentParcelId).OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
    }
}
