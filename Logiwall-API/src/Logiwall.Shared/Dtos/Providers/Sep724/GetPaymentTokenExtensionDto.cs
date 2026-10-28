namespace Logiwall.Shared.Dtos.Providers.Sep724;
public record GetPaymentTokenExtensionDto
{
    [JsonPropertyName("orderId")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("accounts")]
    public List<GetPaymentTokenAccount> Accounts { get; set; }
}
