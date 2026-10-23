namespace Logiwall.Domain.Entities.Emailing;

public class ResetPasswordModel
{
    public string? DisplayName { get; set; }

    public Uri? ResetPasswordLink { get; set; }
}
