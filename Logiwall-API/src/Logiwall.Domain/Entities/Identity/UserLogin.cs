using Microsoft.AspNetCore.Identity;

namespace Logiwall.Domain.Entities.Identity;
public class UserLogin : IdentityUserLogin<int> , ICreationDateAware
{
    public string? Channel { get; set; }

    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;
}
