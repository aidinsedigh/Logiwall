namespace Logiwall.Infrastructure.Options;

public class Sep724Options
{
    public const string SectionName = "Providers:Sep724";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    /// <summary>Value placed after "Basic " in the token request's Authorization header.</summary>
    public string BasicAuthorization { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
}
