using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class OrderParcelConfiguration : IEntityTypeConfiguration<OrderParcel>
{
    public void Configure(EntityTypeBuilder<OrderParcel> builder)
    {
    }
}
