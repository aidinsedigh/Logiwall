namespace Logiwall.Shared.Dtos.OrderItems;

public record ShippingReferenceResponseDto(int Id, string Category, string Title, int? PackType)
{
    public string? ExternalCode { get; init; }
    public short? MappedOrderStatus { get; init; }
}
public record ShipmentParcelResponseDto(int OrderParcelId, string? CourierTrackingCode,
    [property: System.Text.Json.Serialization.JsonPropertyName("courierStatusCode")] string? StatusCode,
    [property: System.Text.Json.Serialization.JsonPropertyName("courierStatus")] string? StatusTitle)
{
    public short? MappedOrderStatus { get; init; }
    public string? MappedOrderStatusName { get; init; }
}
public record ShipmentResponseDto(int Id, int OrderId, string ProviderCode, string State,
    string? ExternalOrderId, DateTimeOffset? RegisteredAt, IReadOnlyList<ShipmentParcelResponseDto> Parcels)
{
    // State is retained for existing clients; new clients use OperationState for submission controls.
    public string OperationState => State;
    [System.Text.Json.Serialization.JsonPropertyName("courierStatusCode")]
    public string? StatusCode { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("courierStatus")]
    public string? StatusTitle { get; init; }
}
public record RegisterShipmentRequestDto
{
    [Range(1, int.MaxValue)] public int OrderId { get; set; }
}
public record CarrierParcelEventRequestDto
{
    [Range(1, long.MaxValue)] public long ParcelEventId { get; set; }
    [Range(1, int.MaxValue)] public int ParcelEventTypeId { get; set; }
    [Required, MaxLength(100)] public string Barcode { get; set; } = string.Empty;
    [MaxLength(200)] public string? EventTypeTitle { get; set; }
    [MaxLength(200)] public string? BarcodeTitle { get; set; }
}
