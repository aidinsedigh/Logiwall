namespace Logiwall.Shared.Dtos.Identity;
public record ConfirmationOtpResponseDto
{
    public string? FullName { get; set; }

    public string? Token { get; set; }

}
