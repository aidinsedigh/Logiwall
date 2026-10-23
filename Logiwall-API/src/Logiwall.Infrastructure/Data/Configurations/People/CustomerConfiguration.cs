using Logiwall.Domain.Entities.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.People;
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasOne(t => t.Person)
            .WithOne(t => t.Customer)
            .HasForeignKey<Customer>(t => t.PersonId)
            .IsRequired();
    }
}
