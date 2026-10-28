
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public record SignInRequestDto
{
    /// <example>09121234567</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.PhoneNumber))]
    public string? UserName { get; set; }

    /// <example>123456</example>
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [NotMapped]
    [Display(Name = nameof(AppStrings.RememberMe))]
    public bool RememberMe { get; set; } = true;

    [Required, StringLength(7)]
    public string? Channel { get; set; } = "pwa";

    public LoginProvider LoginProvider { get; set; }
}
