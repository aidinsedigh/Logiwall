namespace Logiwall.Shared.Dtos.Providers.PostalService;
public class OrderInquiryRequestDto
{
    [JsonPropertyName("originCity")]
    public string OriginCity { get; set; }

    [JsonPropertyName("destinationCity")]
    public string DestinationCity { get; set; }

    [JsonPropertyName("countryId")]
    public string CountryId { get; set; }

    [JsonPropertyName("parcels")]
    public List<OrderInquiryParcelDto>? Parcels { get; set; }
}
