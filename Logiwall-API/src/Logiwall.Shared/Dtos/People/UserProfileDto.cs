using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.People;
public record UserProfileDto
{
    public int? UserId { get; set; }

    public string? Username { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)), MaxLength(100)]
    [Display(Name = nameof(AppStrings.Name))]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError)), MaxLength(100)]
    [Display(Name = nameof(AppStrings.LastName))]
    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    // Identity's own check accepts "a@b"; require a dot in the domain.
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    public PersonKind Kind { get; set; }

    public Gender? Gender { get; set; }

    public DateTimeOffset? BirthDate { get; set; }

    [Required, StringLength(11), NationalCode]
    public string? NationalCode { get; set; }

}
