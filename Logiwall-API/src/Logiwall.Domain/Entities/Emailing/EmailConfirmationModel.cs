namespace Logiwall.Domain.Entities.Emailing;

public record EmailConfirmationModel
{
    public Uri? ConfirmationLink { get; set; }
}
