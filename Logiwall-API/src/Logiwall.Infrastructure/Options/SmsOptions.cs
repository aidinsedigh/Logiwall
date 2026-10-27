namespace Logiwall.Infrastructure.Options;

public class SmsOptions
{
    public const string SectionName = "Sms:PersiaFava";
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
}
