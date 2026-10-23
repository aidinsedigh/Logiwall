namespace Logiwall.Shared.Dtos.People;
public class UpdateCustomerContactRequestDto
{
    [Required]
    public int Id { get; set; }

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

    [Required]
    public int CustomerId { get; set; }

}
