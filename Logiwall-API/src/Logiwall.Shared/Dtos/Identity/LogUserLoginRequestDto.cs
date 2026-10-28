using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Identity;
public record LogUserLoginRequestDto
{
    public string? PhoneNumber { get; set; }

    public string? Channel { get; set; }

    public LoginProvider LoginProvider { get; set; }
}
