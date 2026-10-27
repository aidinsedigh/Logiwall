using Logiwall.Client.Core.Controllers.Dashboard;

namespace Logiwall.Client.Core.Components.Pages.Dashboard;

public partial class ProductsSalesWidget : AppComponentBase
{
    [AutoInject] IDashboardController dashboardController = default!;

    private bool isLoading;
    private BitChart? chart;
    private BitChartConfig config = default!;

    protected override async Task OnInitAsync()
    {
        config = new BitChartConfig
        {
            Type = BitChartType.Bar,
            Options = new BitChartOptions
            {
                Responsive = true,
                Plugins = new() { Legend = new() { Display = false } },
            }
        };

        await GetData();
    }

    private async Task GetData()
    {
        try
        {
            isLoading = true;

            var data = await dashboardController.GetProductsSalesStats(CurrentCancellationToken);

            BitChartDataset chartDataSet = new() { Data = [.. data.Select(d => (double?)d.SaleAmount)] };
            chartDataSet.BackgroundColors = [.. data.Select(d => d.CategoryColor ?? string.Empty)];
            config.Data.Datasets.Add(chartDataSet);
            config.Data.Labels.AddRange(data.Select(d => d.ProductName ?? string.Empty));
        }
        finally
        {
            isLoading = false;
        }
    }
}
