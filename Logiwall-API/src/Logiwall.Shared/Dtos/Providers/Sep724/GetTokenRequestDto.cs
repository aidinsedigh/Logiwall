using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.Sep724;
public record GetTokenRequestDto : IHttpRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }
}
