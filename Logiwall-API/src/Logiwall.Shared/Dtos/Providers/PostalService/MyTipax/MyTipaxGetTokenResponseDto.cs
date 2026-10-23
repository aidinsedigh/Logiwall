using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public class MyTipaxGetTokenResponseDto : IHttpResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
