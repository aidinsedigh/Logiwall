using Logiwall.Domain.Entities.People;

namespace Logiwall.Domain.Entities.OrderItems;
public class Order : ICreationDateAware, IModificationDateAware
{
    public int Id { get; set; }

    public short ChannelId { get; set; }

    public short State { get; set; }

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

    public short ServiceType { get; set; }

    public short ServiceProviderId { get; set; }

    public short ServiceSLA { get; set; }

    [MaxLength(50)]
    public string? ServiceProviderTrackingCode { get; set; }

    public decimal PackagingAmount { get; set; }

    public decimal ServiceAmount { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal Tax { get; set; }

    public decimal FinalAmount { get; }

    public short PickupServiceId { get; set; }

    [Required, MaxLength(15)]
    public string? OrderNumber { get; set; }

    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? ModificationDate { get; set; }

    public DateTimeOffset? PickupDateTime { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = default!;

    public IList<OrderInqueryOption> OrderInqueryOptions { get; set; } = [];

    public IList<OrderParcel> OrderParcels { get; set; } = [];

    public IList<OrderPayment> OrderPayments { get; set; } = [];

    public IList<OrderStatus> OrderStatuses { get; set; } = [];

}
