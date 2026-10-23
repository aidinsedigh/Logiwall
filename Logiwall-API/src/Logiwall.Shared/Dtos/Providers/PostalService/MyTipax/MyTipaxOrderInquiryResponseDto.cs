using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public class MyTipaxOrderInquiryResponseDto : IHttpResponse
{
    [JsonPropertyName("order")]
    public OrderInquiryResponseDto? Order { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("persianMessage")]
    public string? PersianMessage { get; set; }
}
