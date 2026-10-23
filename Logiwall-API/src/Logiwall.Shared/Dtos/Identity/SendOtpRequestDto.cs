using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Identity;
public class SendOtpRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string PhoneNumber { get; set; } = default!;

    [Required, StringLength(7)]
    public string? Channel { get; set; } = "pwa";

    public LoginProvider LoginProvider { get; set; }

}
