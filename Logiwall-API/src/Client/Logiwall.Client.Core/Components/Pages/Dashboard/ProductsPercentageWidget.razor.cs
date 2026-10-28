using Logiwall.Client.Core.Controllers.Dashboard;

namespace Logiwall.Client.Core.Components.Pages.Dashboard;

public partial class ProductsPercentageWidget : AppComponentBase
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChartConfig config = default!;

    protected override async Task OnInitAsync()
    {
        config = new BitChartConfig
        {
            Type = BitChartType.Pie,
            Options = new BitChartOptions
            {
                Responsive = true,
            }
        };

        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;

        try
        {
            
        }
        finally
        {
            isLoading = false;
        }
    }
}
