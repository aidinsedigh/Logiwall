namespace Logiwall.Application.Abstractions.Providers;

public record CarrierAddress(string CityCode, string FullAddress, decimal? Latitude, decimal? Longitude,
    string? Unit, string? PostalCode, string FullName, string Mobile);
public record CarrierParcel(int OrderParcelId, CarrierAddress Origin, CarrierAddress Destination,
    decimal WeightKilograms, decimal ValueRials, decimal Length, decimal Width, decimal Height,
    string PackingCode, string ContentCode, int PackType, decimal CodRials);
public record CarrierShipmentRequest(string TraceCode, IReadOnlyList<CarrierParcel> Parcels,
    int ServiceId, int PaymentType, int PickupType, int DistributionType);
public record CarrierQuote(int ServiceId, decimal AmountRials, string RequestJson);
public record CarrierRegistrationResult(bool Succeeded, bool IsAmbiguous, int? HttpStatus,
    string? ExternalOrderId, IReadOnlyList<string> TrackingCodes, string? ResponseJson, string? ErrorCode,
    Logiwall.Shared.Exceptions.ErrorResourcePayload? ValidationErrors = null, string? ErrorMessage = null,
    string? CarrierStatusCode = null, string? CarrierStatusTitle = null);
public record CarrierReference(string Category, string Code, string Title, string MetadataJson);

public interface IShippingCarrier
{
    string Code { get; }
    Task<IReadOnlyList<CarrierQuote>> QuoteAsync(CarrierShipmentRequest request, CancellationToken cancellationToken);
    string SerializeRegistration(CarrierShipmentRequest request);
    bool MatchesPaymentMethod(string requestJson, Constants.PaymentMethod paymentMethod);
    Task<CarrierRegistrationResult> RegisterAsync(string requestJson, CancellationToken cancellationToken);
    Task<CarrierRegistrationResult> CancelAsync(string externalOrderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CarrierReference>> GetReferencesAsync(CancellationToken cancellationToken);
    /// <summary>Returns null when the carrier does not know the tracking code.</summary>
    Task<Logiwall.Shared.Dtos.OrderItems.ParcelTrackingResponseDto?> TrackAsync(string trackingCode, CancellationToken cancellationToken);
}
