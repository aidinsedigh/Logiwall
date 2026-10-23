using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.PaymentItems;
public class PaymentResponseDto
{
    public int OrderId { get; set; }

    public PaymentStatus Status { get; set; }

    public string? Message { get; set; }
}
