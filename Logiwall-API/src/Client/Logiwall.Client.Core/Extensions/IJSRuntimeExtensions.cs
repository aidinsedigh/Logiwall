namespace Microsoft.JSInterop;

public static class IJSRuntimeExtensions
{
    public static async Task ApplyBodyElementClasses(this IJSRuntime jsRuntime, List<string> cssClasses, Dictionary<string, string> cssVariables)
    {
        await jsRuntime.InvokeVoidAsync("App.applyBodyElementClasses", cssClasses, cssVariables);
    }

    /// <summary>Removes the host page's loading overlay (App.razor) once the client is interactive.</summary>
    public static async Task RemoveLoading(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("App.removeLoading");
    }
}
