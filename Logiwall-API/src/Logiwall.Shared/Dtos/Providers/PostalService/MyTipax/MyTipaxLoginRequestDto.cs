using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public record MyTipaxLoginRequestDto : IHttpRequest
{
    [JsonPropertyName("account")]
    public MyTipaxLoginAccountDto? Account { get; set; }
}
