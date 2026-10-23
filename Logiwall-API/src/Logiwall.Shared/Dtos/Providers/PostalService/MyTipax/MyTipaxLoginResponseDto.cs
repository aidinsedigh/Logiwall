using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public class MyTipaxLoginResponseDto : IHttpResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("persianMessage")]
    public string? PersianMessage { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
