using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Shared.Dtos.Dashboard;

namespace Logiwall.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, Authorize(Policy = "AdminsOnly")]
public partial class DashboardController : AppControllerBase
{
    [AutoInject] private IOrderService orderService = default!;

    [HttpGet]
    public async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken)
    {
        var result = await orderService.GetOrderStatsAsync(cancellationToken);

        return result.Data!;
    }
}
