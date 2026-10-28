namespace Logiwall.Domain.Entities.Emailing;

public record ResetPasswordModel
{
    public string? DisplayName { get; set; }

    public Uri? ResetPasswordLink { get; set; }
}
