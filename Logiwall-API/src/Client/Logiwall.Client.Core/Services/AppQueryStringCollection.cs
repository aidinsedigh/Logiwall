namespace Logiwall.Client.Core.Services;

/// <summary>
/// The dynamic query string of a generated HTTP client proxy. Bit.SourceGenerators 10 requires this type by name.
/// </summary>
public class AppQueryStringCollection() : Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
{
    /// <summary>
    /// Returns <c>key=value</c> pairs joined with <c>&amp;</c>, or null when there is no value, so the proxy adds no <c>?</c>.
    /// </summary>
    public override string? ToString()
    {
        var pairs = this.Where(kv => kv.Value is not null)
                        .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}")
                        .ToArray();

        return pairs.Length == 0 ? null : string.Join('&', pairs);
    }
}
