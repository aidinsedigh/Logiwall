using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Logiwall.Shared.Resources;

namespace Logiwall.UnitTests.Resources;

/// <summary>
/// ADR-0003: each resource key has a Persian resource, so every client message is Persian.
/// </summary>
public class PersianResourceTests
{
    public static TheoryData<string> ResourceTypes => [nameof(AppStrings), nameof(IdentityStrings)];

    [Theory]
    [MemberData(nameof(ResourceTypes))]
    public void Each_neutral_key_has_a_persian_resource(string resourceType)
    {
        var manager = resourceType == nameof(AppStrings) ? AppStrings.ResourceManager : IdentityStrings.ResourceManager;

        // The satellite assembly is "fa"; fa-IR requests fall back to it.
        var persian = ReadSet(manager, new CultureInfo("fa"));
        var missing = ReadSet(manager, CultureInfo.InvariantCulture).Keys
            .Where(key => !IsPersian(persian.GetValueOrDefault(key)))
            .Order()
            .ToList();

        Assert.True(missing.Count == 0, $"{missing.Count} keys have no Persian resource or one with ASCII letters:{string.Join(", ", missing)}");
    }

    // A Persian message is not empty and has no ASCII letter; {0} placeholders are permitted.
    private static bool IsPersian(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(Regex.Replace(value, @"\{\d+\}", ""), "[A-Za-z]");

    private static Dictionary<string, string?> ReadSet(ResourceManager manager, CultureInfo culture) =>
        manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => e.Value as string);
}
