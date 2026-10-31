using Microsoft.Extensions.Caching.Memory;

namespace Logiwall.Infrastructure.Providers;

/// <summary>
/// Caches provider access tokens across requests, so each quote or payment link does not log in again.
/// </summary>
public sealed class ProviderTokenCache(IMemoryCache cache)
{
    private static readonly TimeSpan SafetyMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinimumLifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Returns the cached token, or calls <paramref name="factory"/> and caches a non-empty result
    /// for its lifetime minus a 30 second safety margin (at least 30 seconds).
    /// </summary>
    public async Task<string?> GetOrCreateAsync(string key, Func<Task<(string? Token, TimeSpan Lifetime)>> factory)
    {
        if (cache.TryGetValue(key, out string? cached) && string.IsNullOrEmpty(cached) is false)
            return cached;

        var (token, lifetime) = await factory();
        if (string.IsNullOrEmpty(token))
            return token;

        var expiry = lifetime - SafetyMargin;
        cache.Set(key, token, expiry < MinimumLifetime ? MinimumLifetime : expiry);
        return token;
    }

    public void Invalidate(string key) => cache.Remove(key);
}
