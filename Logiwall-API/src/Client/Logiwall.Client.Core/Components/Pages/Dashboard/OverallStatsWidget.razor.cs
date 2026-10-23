using Logiwall.Client.Core.Controllers.Dashboard;
using Logiwall.Shared.Dtos.Dashboard;

namespace Logiwall.Client.Core.Components.Pages.Dashboard;

public partial class OverallStatsWidget : AppComponentBase
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private OverallAnalyticsStatsDataResponseDto data = new();

    protected override async Task OnInitAsync()
    {
        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;

        try
        {
            data = await dashboardController.GetOverallAnalyticsStatsData(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }
}
