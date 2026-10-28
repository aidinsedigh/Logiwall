namespace Logiwall.Shared.Dtos.OrderItems;
public record SelectPaymentTypeResponseDto
{
    [JsonPropertyName("requestId")]
    public long RequestId { get; set; }

    [JsonPropertyName("referenceCode")]
    public Guid ReferenceCode { get; set; }

    [JsonPropertyName("paymentLink")]
    public string? PaymentLink { get; set; }
}
