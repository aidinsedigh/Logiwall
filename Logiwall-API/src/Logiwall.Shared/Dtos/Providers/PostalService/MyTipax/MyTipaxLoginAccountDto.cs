namespace Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
public class MyTipaxLoginAccountDto
{
    [JsonPropertyName("cellNumber")]
    public string? CellPhoneNumber { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
