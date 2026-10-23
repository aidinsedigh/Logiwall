using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Identity;
public class ConfirmaionOtpRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string PhoneNumber { get; set; } = default!;

    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string Code { get; set; } = default!;

    [Required, StringLength(7)]
    public string? Channel { get; set; } = "pwa";

    public LoginProvider LoginProvider { get; set; }
}
