using Logiwall.Domain.Entities.Identity;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Domain.Entities.People;
public class UserOtp : ICreationDateAware
{
    public int Id { get; set; }

    [Required, MaxLength(5)]
    public string Code { get; set; } = default!;

    public bool Active { get; set; } = true;

    [Required, StringLength(7)]
    public string? Channel { get; set; } = "pwa";

    public LoginProvider LoginProvider { get; set; }

    public DateTimeOffset CreationDate { get; set; }

    public DateTimeOffset ExpirationDate { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = default!;
}
