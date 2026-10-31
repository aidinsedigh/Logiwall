using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.Sep724;
public record GetPaymentTokenRequestDto<T> : IHttpRequest
{
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("mobileNo")]
    public string? MobileNo { get; set; }

    [JsonPropertyName("terminal")]
    public string? Terminal { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; set; }

    [JsonPropertyName("extensions")]
    public T? Extensions { get; set; }
}
