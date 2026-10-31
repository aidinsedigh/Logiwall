using Logiwall.Client.Core.Controllers.Identity;
using Logiwall.Shared.Dtos.Identity;

namespace Logiwall.Client.Core.Components.Pages.Identity;

[Authorize(Policy = "AdminsOnly")]
public partial class EditProfilePage : AppComponentBase
{
    [AutoInject] IUserController userController = default!;

    private bool isSaving;
    private bool isLoading;
    private string? editProfileMessage;
    //private BitMessageBarType editProfileMessageType;
    private UserDto user = new();
    private readonly EditUserDto userToEdit = new();
    private bool isDeleteAccountConfirmModalOpen;

    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            await LoadEditProfileData();
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }

    private async Task LoadEditProfileData()
    {
        user = await GetCurrentUser() ?? new();

        user.Patch(userToEdit);
    }

    private Task<UserDto> GetCurrentUser() => userController.GetCurrentUser(CurrentCancellationToken);

    private async Task DoSave()
    {
        if (isSaving) return;

        isSaving = true;
        editProfileMessage = null;

        try
        {
            userToEdit.Patch(user);

            (await userController.Update(userToEdit, CurrentCancellationToken)).Patch(user);

            PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, user);

            //editProfileMessageType = BitMessageBarType.Success;
            editProfileMessage = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            //editProfileMessageType = BitMessageBarType.Error;

            editProfileMessage = e.Message;
        }
        finally
        {
            isSaving = false;
        }
    }
}
