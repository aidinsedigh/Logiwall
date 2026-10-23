namespace Logiwall.Shared.Dtos.OrderItems;
public class SelectOptionResponseDto
{
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    [JsonPropertyName("referenceCode")]
    public Guid ReferenceCode { get; set; }

    [JsonPropertyName("paymentLink")]
    public string? PaymentLink { get; set; }
}
