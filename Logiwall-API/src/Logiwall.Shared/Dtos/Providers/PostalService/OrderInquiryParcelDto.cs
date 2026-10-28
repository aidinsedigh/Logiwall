namespace Logiwall.Shared.Dtos.Providers.PostalService;
public record OrderInquiryParcelDto
{
    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("weight")]
    public string? Weight { get; set; }

    [JsonPropertyName("needPacking")]
    public string NeedPacking { get; set; }

    [JsonPropertyName("lockerSupport")]
    public string LockerSupport { get; set; }

    [JsonPropertyName("parcelType")]
    public string? ParcelType { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}
