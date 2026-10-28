namespace Logiwall.Shared.Dtos.Providers.Sep724;
public record GetPaymentTokenAccount
{
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("iban")]
    public string? Iban { get; set; }
}
