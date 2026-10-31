namespace Logiwall.Shared.Dtos.Providers.PostalService;
public record OrderInquiryResponseDto
{
    [JsonPropertyName("options")]
    public  List<OrderInquiryOptionDto>? Options { get; set; }
}
