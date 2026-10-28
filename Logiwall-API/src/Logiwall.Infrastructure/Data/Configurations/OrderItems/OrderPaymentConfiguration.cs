using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        builder.HasIndex(p => p.GatewayReferenceCode).IsUnique().HasFilter("[GatewayReferenceCode] IS NOT NULL");
    }
}
