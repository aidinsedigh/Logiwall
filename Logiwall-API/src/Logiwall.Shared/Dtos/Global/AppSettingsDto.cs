namespace Logiwall.Shared.Dtos.Global;
public record AppSettingsDto
{
    public string? LoginOtpText { get; set; }

    public string? SepConnectUrl { get; set; }

    public string? SepConnectScope { get; set; }

    public string? SepPayBaseUrl { get; set; }

    public string? MyTipaxBaseUrl { get; set; }

    public string? MyTipaxInquiryVersion { get; set; }

    public string? MyTipaxIban { get; set; }

    public string? MyTipaxShare { get; set; }

    public string? LogiwallIban { get; set; }

    public string? LogiwallShare { get; set; }
}
