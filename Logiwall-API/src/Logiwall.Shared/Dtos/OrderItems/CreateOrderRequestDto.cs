namespace Logiwall.Shared.Dtos.OrderItems;
public class CreateOrderRequestDto
{
    public short ChannelId { get; set; }

    [Required, MaxLength(100)]
    public string SenderName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string SenderLastName { get; set; } = default!;

    [Required, MaxLength(15)]
    public string SenderPhoneNumber { get; set; } = default!;

    public short OriginCity { get; set; }

    [Required, MaxLength(100)]
    public string OriginLat { get; set; } = default!;

    [Required, MaxLength(100)]
    public string OriginLng { get; set; } = default!;

    [Required, MaxLength(500)]
    public string OriginAddress { get; set; } = default!;

    [MaxLength(10)]
    public string? OriginPostalCode { get; set; }

    [Required, MaxLength(50)]
    public string OriginalPlaque { get; set; } = default!;

    [Required, MaxLength(50)]
    public string OriginalUnit { get; set; } = default!;

    [Required, MaxLength(100)]
    public string ReceiverName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string ReceiverLastName { get; set; } = default!;

    [Required, MaxLength(15)]
    public string ReceiverPhoneNumber { get; set; } = default!;

    public short? DestinationCountry { get; set; }

    public short DestinationCity { get; set; }

    [Required, MaxLength(100)]
    public string DestinationLat { get; set; } = default!;

    [Required, MaxLength(100)]
    public string DestinationLng { get; set; } = default!;

    [Required, MaxLength(500)]
    public string DestinationAddress { get; set; } = default!;

    [MaxLength(10)]
    public string? DestinationPostalCode { get; set; }

    [Required, MaxLength(50)]
    public string DestinationPlaque { get; set; } = default!;

    [Required, MaxLength(50)]
    public string DestinationUnit { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public IList<CreateOrderParcelRequestDto> OrderParcels { get; set; } = default;
}
