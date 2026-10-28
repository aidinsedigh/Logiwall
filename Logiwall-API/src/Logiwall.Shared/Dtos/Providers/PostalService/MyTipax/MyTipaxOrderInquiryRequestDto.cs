using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public record MyTipaxOrderInquiryRequestDto : IHttpRequest
{
    [JsonPropertyName("order")]
    public OrderInquiryRequestDto? Order { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
