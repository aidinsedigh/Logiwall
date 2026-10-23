namespace Logiwall.Shared.Dtos.Providers.Sep724;
public class GetPaymentTokenResponseDto
{
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    [JsonPropertyName("referenceCode")]
    public Guid ReferenceCode { get; set; }

    [JsonPropertyName("paymentLink")]
    public string? PaymentLink { get; set; }
}
