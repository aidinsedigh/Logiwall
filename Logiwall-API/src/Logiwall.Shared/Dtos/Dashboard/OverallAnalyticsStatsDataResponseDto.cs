namespace Logiwall.Shared.Dtos.Dashboard;

/// <summary>The signed-in user's own order counts.</summary>
public record OverallAnalyticsStatsDataResponseDto
{
    public int TotalOrders { get; set; }

    public int Last30DaysOrders { get; set; }

    /// <summary>Orders with a successful payment.</summary>
    public int PaidOrders { get; set; }
}
