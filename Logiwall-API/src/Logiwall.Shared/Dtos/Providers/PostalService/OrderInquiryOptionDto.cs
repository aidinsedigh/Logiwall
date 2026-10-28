namespace Logiwall.Shared.Dtos.Providers.PostalService;
public record OrderInquiryOptionDto
{
    [JsonPropertyName("id")]
    public string? ProviderId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("specialService")]
    public string? SpecialService { get; set; }

    [JsonPropertyName("shippingCost")]
    public string? ShippingCost { get; set; }

    [JsonPropertyName("packageService")]
    public string? PackageService { get; set; }

    [JsonPropertyName("tax")]
    public string? Tax { get; set; }

    [JsonPropertyName("finalAmount")]
    public string? FinalAmount { get; set; }

    [JsonPropertyName("delivereDate")]
    public string? DelivereDate { get; set; }

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("cod")]
    public string? Cod { get; set; }
}
