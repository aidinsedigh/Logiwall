using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Shared.Dtos.People;
public class CreatePersonRequestDto
{
    public string Name { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public int UserId { get; set; }

    public string PhoneNumber { get; set; } = default!;

    public string NationalCode { get; set; } = default!;

    public string? Email { get; set; }
}
