using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Domain.Entities.OrderItems;
public class OrderPayment : ICreationDateAware
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public int? MerchantId { get; set; }

    public int? TerminalId { get; set; }

    [Required, StringLength(25)]
    public string PayTrackingCode { get; set; }

    public PaymentType PaymentType { get; set; }

    public PaymentStatus Status { get; set; }

    [Required, StringLength(25)]
    public DateTimeOffset PayDate { get; set; }

    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;

    public long? GatewayRequestId { get; set; }

    public Guid? GatewayReferenceCode { get; set; }

    /// <summary>The amount the gateway reported when it verified the payment.</summary>
    public long? VerifiedAmount { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }


    public int OrderId { get; set; }

    public Order Order { get; set; } = default!;
}
