namespace Logiwall.Infrastructure.Options;

public class MyTipaxOptions
{
    public const string SectionName = "Providers:MyTipax";
    public string TokenUsername { get; set; } = string.Empty;
    public string TokenPassword { get; set; } = string.Empty;
    public string LoginUsername { get; set; } = string.Empty;
    public string LoginPassword { get; set; } = string.Empty;
}
