namespace Logiwall.Domain.Entities.OrderItems;

public enum ShipmentRegistrationState { Prepared, Submitting, Registered, Rejected, Unknown, Cancelled }

public class ProviderReferenceItem
{
    public int Id { get; set; }
    public short ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; } = default!;
    public string Category { get; set; } = string.Empty;
    public OrderStatusCode? MappedOrderStatus { get; set; }
    public string ExternalCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}

public class Shipment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public short ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; } = default!;
    public int QuoteId { get; set; }
    public string TraceCode { get; set; } = string.Empty;
    public ShipmentRegistrationState State { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string? ExternalOrderId { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<ShipmentParcel> Parcels { get; set; } = new List<ShipmentParcel>();
    public ICollection<ShipmentAttempt> Attempts { get; set; } = new List<ShipmentAttempt>();
}

public class ShipmentParcel
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = default!;
    public int OrderParcelId { get; set; }
    public int Position { get; set; }
    public string? TrackingCode { get; set; }
    public string? ExternalStatusCode { get; set; }
    public OrderStatusCode? MappedOrderStatus { get; set; }
    public string? StatusTitle { get; set; }
    public long? LastExternalEventId { get; set; }
    public DateTimeOffset? LastStatusAt { get; set; }
}

public class ShipmentAttempt
{
    public long Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = default!;
    public string Operation { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class ShipmentEvent
{
    public long Id { get; set; }
    public short ServiceProviderId { get; set; }
    public long ExternalEventId { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string? StatusTitle { get; set; }
    public int? ShipmentParcelId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? LastReconciledAt { get; set; }
    public bool Applied { get; set; }
    public bool Processed { get; set; }
}

public class ShippingJobLease
{
    public string Name { get; set; } = string.Empty;
    public Guid Owner { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

