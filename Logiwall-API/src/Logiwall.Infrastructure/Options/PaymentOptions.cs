namespace Logiwall.Infrastructure.Options;

public class PaymentOptions
{
    public const string SectionName = "Payment";
    /// <summary>Origins (scheme://host[:port]) the gateway may redirect back to. Empty allows any (development only).</summary>
    public string[] AllowedCallbackOrigins { get; set; } = [];
}
