using Logiwall.Domain.Entities.People;
using Microsoft.AspNetCore.Identity;

namespace Logiwall.Domain.Entities.Identity;

public class User : IdentityUser<int>, IArchiveAware
{
    public bool IsArchive { get; set; }

    public bool Active { get; set; }

    public Person? Person { get; set; }

    //public DateTimeOffset? ConfirmationEmailRequestedOn { get; set; }
    //public DateTimeOffset? ResetPasswordEmailRequestedOn { get; set; }
}
