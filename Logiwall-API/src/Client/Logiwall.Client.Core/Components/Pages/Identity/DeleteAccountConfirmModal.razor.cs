using Logiwall.Client.Core.Controllers.Identity;

namespace Logiwall.Client.Core.Components.Pages.Identity;

public partial class DeleteAccountConfirmModal : AppComponentBase
{
    [AutoInject] IUserController userController = default!;

    [Parameter] public bool IsOpen { get; set; }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    private async Task CloseModal()
    {
        IsOpen = false;

        await IsOpenChanged.InvokeAsync(false);
    }

    private async Task DeleteAccount()
    {
        await userController.Delete(CurrentCancellationToken);

        await AuthenticationManager.SignOut();

        await CloseModal();
    }
}
