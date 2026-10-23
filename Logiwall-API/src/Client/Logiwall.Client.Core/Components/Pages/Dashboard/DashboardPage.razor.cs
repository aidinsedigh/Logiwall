using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Logiwall.Client.Core.Components.Pages.Dashboard;

[Authorize]
public partial class DashboardPage : AppComponentBase
{
    [AutoInject] LazyAssemblyLoader lazyAssemblyLoader = default!;

    private bool isLoadingAssemblies = true;

    protected async override Task OnInitAsync()
    {
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(["Newtonsoft.Json.wasm"]);
            }
        }
        finally
        {
            isLoadingAssemblies = false;
        }

        await base.OnInitAsync();
    }
}
