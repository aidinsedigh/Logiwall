using System.ComponentModel;
using System.Reflection;

namespace Logiwall.Shared.Extensions;
public static class EnumExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        FieldInfo field = enumValue.GetType().GetField(enumValue.ToString());

        DescriptionAttribute attribute = (DescriptionAttribute)field
            .GetCustomAttribute(typeof(DescriptionAttribute));

        return attribute?.Description ?? enumValue.ToString();
    }
}
