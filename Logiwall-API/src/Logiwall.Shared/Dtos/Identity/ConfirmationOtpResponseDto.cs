namespace Logiwall.Shared.Dtos.Identity;
public record ConfirmationOtpResponseDto
{
    public string? FullName { get; set; }

    public string? AccessToken { get; set; }

    /// <summary>
    /// In seconds.
    /// </summary>
    public long ExpiresIn { get; set; }
}
