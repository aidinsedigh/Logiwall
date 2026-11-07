namespace Logiwall.Shared.Dtos.OrderItems;

/// <summary>One row of <c>GetOrders</c>. It has only the fields the front's order card and details dialog read.</summary>
public record OrderDto
{
    public int Id { get; set; }

    public short Status { get; set; }

    public string? StatusName { get; set; }

    public string SenderName { get; set; } = default!;

    public string SenderLastName { get; set; } = default!;

    public string SenderPhoneNumber { get; set; } = default!;

    public string? OriginCityName { get; set; }

    public string OriginAddress { get; set; } = default!;

    public string? OriginPostalCode { get; set; }

    public string OriginalPlaque { get; set; } = default!;

    public string OriginalUnit { get; set; } = default!;

    public string ReceiverName { get; set; } = default!;

    public string ReceiverLastName { get; set; } = default!;

    public string ReceiverPhoneNumber { get; set; } = default!;

    public string? DestinationCityName { get; set; }

    public string DestinationAddress { get; set; } = default!;

    public string? DestinationPostalCode { get; set; }

    public string DestinationPlaque { get; set; } = default!;

    public string DestinationUnit { get; set; } = default!;

    public string? Description { get; set; }

    public short ServiceProviderId { get; set; }

    public string? ServiceProviderName { get; set; }

    /// <summary>The carrier's tracking code; null until the carrier registers the shipment.</summary>
    public string? ServiceProviderTrackingCode { get; set; }

    /// <summary>The chosen quote's total; 0 before an option is selected.</summary>
    public decimal FinalAmount { get; set; }

    public string PaymentStatus { get; set; } = nameof(OrderPaymentState.Unpaid);
    public string? PaymentStatusName { get; set; }

    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? ModificationDate { get; set; }

    public DateTimeOffset? PickupDateTime { get; set; }
}
