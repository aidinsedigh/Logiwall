namespace Logiwall.Infrastructure.Options;

/// <summary>
/// The first admin of the Blazor panel (ADR-0005). Set it with user-secrets or environment
/// variables (<c>AdminSeed__PhoneNumber</c>, <c>AdminSeed__Password</c>), never in appsettings.
/// </summary>
public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
}
