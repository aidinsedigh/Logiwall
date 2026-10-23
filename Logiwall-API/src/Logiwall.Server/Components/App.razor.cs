using Microsoft.AspNetCore.Components;

namespace Logiwall.Server.Components;

[StreamRendering(enabled: true)]
public partial class App : ComponentBase
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;
}
