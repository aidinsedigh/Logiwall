using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Shared.Dtos.Providers.Sep724;
public class SepResponseBase<T> : IHttpResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }

    [JsonPropertyName("Title")]
    public string? Title { get; set; }

    [JsonPropertyName("Status")]
    public int Status { get; set; }

    [JsonPropertyName("Detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}
