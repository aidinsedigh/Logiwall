namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public record MyTipaxLoginAccountDto
{
    [JsonPropertyName("cellNumber")]
    public string? CellPhoneNumber { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
