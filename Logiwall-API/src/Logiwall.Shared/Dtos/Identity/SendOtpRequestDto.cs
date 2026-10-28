using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Identity;
public record SendOtpRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    [RegularExpression(@"^09\d{9}$")]
    public string PhoneNumber { get; set; } = default!;

    [Required, StringLength(7)]
    public string? Channel { get; set; } = "pwa";

    public LoginProvider LoginProvider { get; set; }

}
