
namespace Logiwall.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public record ConfirmEmailRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? Token { get; set; }
}
