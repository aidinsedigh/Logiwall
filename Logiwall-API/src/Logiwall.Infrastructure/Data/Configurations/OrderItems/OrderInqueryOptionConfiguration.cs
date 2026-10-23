using Logiwall.Domain.Entities.OrderItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.OrderItems;
public class OrderInqueryOptionConfiguration : IEntityTypeConfiguration<OrderInqueryOption>
{
    public void Configure(EntityTypeBuilder<OrderInqueryOption> builder)
    {
    }
}
