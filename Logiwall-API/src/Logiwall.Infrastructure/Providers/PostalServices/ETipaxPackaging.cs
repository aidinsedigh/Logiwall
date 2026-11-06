using System.Globalization;
using System.Text.Json;

namespace Logiwall.Infrastructure.Providers.PostalServices;

internal static class ETipaxPackaging
{
    // ShopLink's TipaxPackagingTypes maps both carrier records to None.
    public static bool IsNoPackaging(string code) => code is "504" or "521";

    public static int? PackType(string metadata)
    {
        using var json = JsonDocument.Parse(metadata);
        return json.RootElement.TryGetProperty("packType", out var value)
            && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            && number > 0 ? number : null;
    }
}
