using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.People;
public class UserProfileDto
{
    public int? UserId { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public PersonKind Kind { get; set; }

    public Gender? Gender { get; set; }

    public DateTimeOffset? BirthDate { get; set; }

    [Required, StringLength(11)]
    public string? NationalCode { get; set; }

}
