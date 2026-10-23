namespace Logiwall.Shared.Dtos.Providers.PostalService;
public class OrderInquiryResponseDto
{
    [JsonPropertyName("options")]
    public  List<OrderInquiryOptionDto>? Options { get; set; }
}
