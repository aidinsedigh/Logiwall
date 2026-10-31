using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logiwall.Shared.Dtos.Identity;

namespace Logiwall.Shared.Dtos.People;
public record SignInWithOtpRequestDto
{
    public string PhoneNumber { get; set; } = default!;
}
