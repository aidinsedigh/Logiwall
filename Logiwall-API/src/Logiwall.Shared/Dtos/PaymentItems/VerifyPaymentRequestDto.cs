namespace Logiwall.Shared.Dtos.PaymentItems;
public record VerifyPaymentRequestDto
{
    [Required]
    public int OrderId { get; set; }
}
