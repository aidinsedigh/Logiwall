using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(t => t.Person)
            .WithOne()
            .HasForeignKey<Person>(t => t.UserId)
            .IsRequired();

        //builder.HasIndex(t => new { t.UserName, t.IsArchive })
        //    .IsUnique()
        //    .HasFilter("[IsArchive] = 0");

        //builder.HasIndex(t => new { t.NormalizedUserName, t.IsArchive })
        //    .IsUnique()
        //    .HasFilter("[IsArchive] = 0");
    }
}
