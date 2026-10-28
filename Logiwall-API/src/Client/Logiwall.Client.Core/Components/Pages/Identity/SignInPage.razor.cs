using Logiwall.Shared.Dtos.Identity;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Client.Core.Components.Pages.Identity;

public partial class SignInPage : AppComponentBase
{
    private bool isLoading;
    private string? signInMessage;
    private readonly SignInRequestDto signInModel = new() { LoginProvider = LoginProvider.Logiwall };

    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    private async Task DoSignIn()
    {
        if (isLoading) return;

        isLoading = true;
        signInMessage = null;

        try
        {
            await AuthenticationManager.SignIn(signInModel, CurrentCancellationToken);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            signInMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
