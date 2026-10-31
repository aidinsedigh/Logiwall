using Microsoft.AspNetCore.Components.Web;

namespace Logiwall.Client.Core.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private bool disposed;
    private bool isMenuOpen;
    private bool isUserAuthenticated;
    private ErrorBoundary errorBoundaryRef = default!;

    [AutoInject] private IPrerenderStateService prerenderStateService = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private AuthenticationManager authManager = default!;

    [AutoInject] private IJSRuntime jsRuntime = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            authManager.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            isUserAuthenticated = await prerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.IsAuthenticated());

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await jsRuntime.RemoveLoading();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            isUserAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleMenuHandler()
    {
        isMenuOpen = !isMenuOpen;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        authManager.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        disposed = true;
    }
}
