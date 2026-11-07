namespace Logiwall.Shared.Dtos.OrderItems;

public record TrackParcelRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)), MaxLength(50)]
    public string ProviderCode { get; set; } = string.Empty;
    // Each carrier has its own format: eTipax uses 21 digits or a 13-character barcode such as BA800197458IR.
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [RegularExpression("^\\s*[A-Za-z0-9]{5,40}\\s*$", ErrorMessage = "ParcelTrackingCodeInvalid")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>The same tracking contract for every carrier. Each carrier adapter maps its own reply into it.</summary>
public record ParcelTrackingResponseDto(string TrackingCode, string? Status, string? ServiceName, DateTimeOffset? RegisteredAt,
    ParcelTrackingPartyDto Sender, ParcelTrackingPartyDto Receiver,
    IReadOnlyList<ParcelTrackingEventDto> Journey, ParcelTrackingPaymentDto Payment)
{
    public string ProviderCode { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
}
public record ParcelTrackingPartyDto(string? FullName, string? City, string? Province);
/// <summary>One step of the parcel journey. The journey is sorted oldest first.</summary>
public record ParcelTrackingEventDto(string Title, DateTimeOffset? OccurredAt, string? City, string? Location, string? Description);
/// <summary>Amounts are in rials, as the carrier reports them.</summary>
public record ParcelTrackingPaymentDto(string? Method, decimal? TotalAmount, decimal? PayOnDeliveryAmount, decimal? CodAmount);
