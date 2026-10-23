using Logiwall.Domain.Entities.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.People;
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasIndex(t => new { t.PhoneNumber, t.IsArchive })
            .IsUnique()
            .HasFilter("[IsArchive] = 0");

        builder.HasIndex(t => new { t.UserId, t.IsArchive })
            .IsUnique()
            .HasFilter("[IsArchive] = 0");
    }
}
