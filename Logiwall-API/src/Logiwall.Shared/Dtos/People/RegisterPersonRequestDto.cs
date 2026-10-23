using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Shared.Dtos.People;
public class RegisterPersonRequestDto
{
    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string LastName { get; set; } = default!;

    [Required]
    public string NationalCode { get; set; } = default!;

    public string? Email { get; set; }

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [MinLength(6, ErrorMessage = nameof(AppStrings.MinLengthAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Password))]
    public string? Password { get; set; }

    [NotMapped]
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [Compare(nameof(Password), ErrorMessage = nameof(AppStrings.CompareAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.ConfirmNewPassword))]
    public string? ConfirmPassword { get; set; }
}
