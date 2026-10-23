using Logiwall.Shared.Dtos.Dashboard;
using Logiwall.Client.Core.Controllers.Dashboard;

namespace Logiwall.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class DashboardController : AppControllerBase, IDashboardController
{
    [HttpGet]
    public async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken)
    {
        var result = new OverallAnalyticsStatsDataResponseDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        return result;
    }
}
