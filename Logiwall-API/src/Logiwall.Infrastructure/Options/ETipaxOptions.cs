namespace Logiwall.Infrastructure.Options;

public class ETipaxOptions
{
    public const string SectionName = "Providers:ETipax";
    public string BaseUrl { get; set; } = "https://omtestapi.tipax.ir";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool ReferenceSyncEnabled { get; set; }
    public int ReferenceSyncMinutes { get; set; } = 1440;
    public bool ReconcileEventsEnabled { get; set; }
    /// <summary>The eTipax "jet" tracking API. It is a different host from <see cref="BaseUrl"/> and uses its own long-lived token.</summary>
    public string TrackingBaseUrl { get; set; } = "http://jet-test.tipax.ir:81";
    public string TrackingToken { get; set; } = string.Empty;
}
