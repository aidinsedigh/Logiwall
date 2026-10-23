using Logiwall.Domain.Entities.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.People;
public class PersonalInfoConfiguration : IEntityTypeConfiguration<PersonalInfo>
{
    public void Configure(EntityTypeBuilder<PersonalInfo> builder)
    {
        builder.HasIndex(t => t.NationalCode).IsUnique();

        builder.HasOne(t => t.Person)
            .WithOne(t => t.PersonalInfo)
            .HasForeignKey<PersonalInfo>(t => t.PersonId)
            .IsRequired();
    }
}
