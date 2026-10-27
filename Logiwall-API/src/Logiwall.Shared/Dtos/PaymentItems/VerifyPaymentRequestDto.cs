namespace Logiwall.Shared.Dtos.PaymentItems;
public class VerifyPaymentRequestDto
{
    [Required]
    public int OrderId { get; set; }
}
