using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Shared.Dtos.People;
public record CreateCustomerContactRequestDto
{
    [Required, MaxLength(100)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Lan { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Lat { get; set; } = default!;

    [Required]
    public short CountryId { get; set; }

    [Required]
    public short CityId { get; set; }

    [Required, MaxLength(10)]
    public string PostalCode { get; set; } = default!;

    [Required, MaxLength(500)]
    public string Address { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Unit { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Plaque { get; set; } = default!;

    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = default!;

    public bool IsDefualt { get; set; }

    public int UserId { get; set; }
}
