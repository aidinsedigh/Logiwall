using System.Reflection;
using Logiwall.Domain.Entities.Global;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Logiwall.Infrastructure.Data.Configurations.Global;
public class ConstantConfiguration
{
    public void Configure(EntityTypeBuilder<Constant> builder)
    {
        var constants = InitialValues();

        builder
            .HasIndex(new[]
            {
                nameof(Constant.Type),
                nameof(Constant.Name),
            })
            .IsUnique();

        builder.HasData(constants);
    }

    private IList<Constant> InitialValues()
    {
        var constantEntities = new List<Constant>();

        Type constantType = typeof(Constants);
        MemberInfo[] membersInfo = constantType.GetMembers(BindingFlags.Public);

        foreach (var memberInfo in membersInfo)
        {
            Type enumType = (Type)memberInfo;
            var enumValues = enumType.GetEnumValues();

            foreach (object enumValue in enumValues)
            {
                object underlyingValue = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()));

                constantEntities.Add(new Constant
                {
                    Code = Convert.ToInt16(underlyingValue),
                    Type = enumType.Name,
                    Name = enumValue.ToString()!
                });
            }
        }

        return constantEntities;
    }
}
