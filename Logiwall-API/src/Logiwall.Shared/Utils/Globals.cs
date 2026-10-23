using System.Reflection;

namespace Logiwall.Shared.Utils;
public static partial class Globals
{
    public static IDictionary<string, object?>? ObjectToDictionary(object value)
    {
        if (value is IDictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (value is not null)
        {
            dictionary = value.GetType().GetProperties().ToDictionary(t => t.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? t.Name, t => t.GetValue(value, null));
        }

        return dictionary;
    }

    public static string ConvertPersianNumbersToEnglish(string input)
    {
        var persianNumbers = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var englishNumbers = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        for (int i = 0; i < persianNumbers.Length; i++)
        {
            input = input.Replace(persianNumbers[i], englishNumbers[i]);
        }

        return input;
    }
}
