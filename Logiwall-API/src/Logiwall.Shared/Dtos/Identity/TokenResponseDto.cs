
namespace Logiwall.Shared.Dtos.Identity;

public record TokenResponseDto
{
    public string? TokenType { get; set; }

    public string? AccessToken { get; set; }

    /// <summary>
    /// In seconds.
    /// </summary>
    public long ExpiresIn { get; set; }

    public string? RefreshToken { get; set; }
}
