namespace Logiwall.Shared.Dtos.Providers.Sep724;
public class GetPaymentInfoResponseDto
{
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    [JsonPropertyName("mPin")]
    public string? MPin { get; set; }

    [JsonPropertyName("paymentParam")]
    public string? PaymentParam { get; set; }

    [JsonPropertyName("referenceCode")]
    public Guid ReferenceCode { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }
}
