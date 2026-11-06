using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;

public class ShippingConfiguration : IEntityTypeConfiguration<Shipment>, IEntityTypeConfiguration<ShipmentParcel>,
    IEntityTypeConfiguration<ShipmentAttempt>, IEntityTypeConfiguration<ShipmentEvent>,
    IEntityTypeConfiguration<ProviderReferenceItem>, IEntityTypeConfiguration<ShippingJobLease>
{
    public void Configure(EntityTypeBuilder<Shipment> b)
    {
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasIndex(x => x.TraceCode).IsUnique();
        b.Property(x => x.TraceCode).HasMaxLength(100);
        b.Property(x => x.ExternalOrderId).HasMaxLength(100);
        b.Property(x => x.LastErrorCode).HasMaxLength(80);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ServiceProvider).WithMany().HasForeignKey(x => x.ServiceProviderId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ShipmentParcel> b)
    {
        b.Property(x => x.TrackingCode).HasMaxLength(100);
        b.Property(x => x.ExternalStatusCode).HasMaxLength(50);
        b.Property(x => x.StatusTitle).HasMaxLength(200);
        b.HasIndex(x => new { x.ShipmentId, x.Position }).IsUnique();
        b.HasIndex(x => x.TrackingCode);
        b.HasOne<OrderParcel>().WithMany().HasForeignKey(x => x.OrderParcelId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ShipmentAttempt> b)
    {
        b.Property(x => x.Operation).HasMaxLength(30);
        b.Property(x => x.ErrorCode).HasMaxLength(80);
    }
    public void Configure(EntityTypeBuilder<ShipmentEvent> b)
    {
        b.Property(x => x.TrackingCode).HasMaxLength(100);
        b.Property(x => x.StatusCode).HasMaxLength(50);
        b.Property(x => x.StatusTitle).HasMaxLength(200);
        b.HasIndex(x => new { x.ServiceProviderId, x.ExternalEventId }).IsUnique();
        b.HasIndex(x => new { x.Processed, x.ReceivedAt });
        b.HasOne<ShipmentParcel>().WithMany().HasForeignKey(x => x.ShipmentParcelId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ProviderReferenceItem> b)
    {
        b.Property(x => x.Category).HasMaxLength(40);
        b.Property(x => x.ExternalCode).HasMaxLength(100);
        b.Property(x => x.Title).HasMaxLength(200);
        b.HasIndex(x => new { x.ServiceProviderId, x.Category, x.ExternalCode }).IsUnique();
        b.HasOne(x => x.ServiceProvider).WithMany().HasForeignKey(x => x.ServiceProviderId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ShippingJobLease> b)
    {
        b.HasKey(x => x.Name);
        b.Property(x => x.Name).HasMaxLength(100);
    }
}
