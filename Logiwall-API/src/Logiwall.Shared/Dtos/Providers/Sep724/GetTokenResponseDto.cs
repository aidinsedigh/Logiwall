using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.Sep724;
public class GetTokenResponseDto : IHttpResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpireIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}
