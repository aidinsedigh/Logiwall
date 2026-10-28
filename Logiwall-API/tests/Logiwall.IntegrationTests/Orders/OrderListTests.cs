using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Orders;

// Tests for plan 014: the order list is ordered, bounded and accepts the frontend's filters.
[Collection("api")]
public class OrderListTests(SqlServerFixture fixture)
{
    private async Task<(HttpClient Client, List<int> OrderIds)> ThreeOrdersAsync()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var ids = new List<int>();
        for (var i = 0; i < 3; i++)
            ids.Add(await OrderTestData.InsertOrderAsync(fixture, customerId));
        return (client, ids);
    }

    private static async Task<List<int>> GetOrderIdsAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("api/Order/GetOrders", body);
        response.EnsureSuccessStatusCode();
        var data = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result");
        return data.EnumerateArray().Select(o => o.GetProperty("id").GetInt32()).ToList();
    }

    [Fact]
    public async Task Page_has_the_total_count_of_all_matching_orders()
    {
        var (client, _) = await ThreeOrdersAsync();

        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { hasPagination = true, pageNumber = 1, pageSize = 2 });

        var data = (await ApiClient.ReadJsonAsync(response)).GetProperty("data");
        Assert.Equal(3, data.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, data.GetProperty("result").GetArrayLength());
    }

    [Fact]
    public async Task Order_has_city_names_and_the_chosen_total()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        (await OrderTestData.SelectAsync(client, orderId, await OrderTestData.QuoteAsync(client, orderId))).EnsureSuccessStatusCode();
        var cityName = await fixture.WithDbAsync(db => db.StaticDatas.Where(s => s.Id == 1).Select(s => s.Title).SingleOrDefaultAsync());

        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });

        var order = Assert.Single((await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray());
        Assert.NotNull(cityName); // the migrations seed StaticDatas, so row 1 exists
        Assert.Equal(1100m, order.GetProperty("finalAmount").GetDecimal()); // FakePostalProvider's total
        Assert.Equal(cityName, order.GetProperty("originCityName").GetString());
        Assert.Equal(cityName, order.GetProperty("destinationCityName").GetString());
        Assert.False(order.TryGetProperty("orderParcels", out _));
    }

    [Fact]
    public async Task First_page_returns_the_newest_orders_first()
    {
        var (client, ids) = await ThreeOrdersAsync();

        var page = await GetOrderIdsAsync(client, new { hasPagination = true, pageNumber = 1, pageSize = 2 });

        Assert.Equal(ids.OrderByDescending(id => id).Take(2), page);
    }

    [Fact]
    public async Task Filter_on_origin_city_returns_only_matching_orders()
    {
        var (client, ids) = await ThreeOrdersAsync();
        var target = ids[1];
        await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == target).ExecuteUpdateAsync(s => s.SetProperty(o => o.OriginCity, (short)7)));

        var result = await GetOrderIdsAsync(client, new
        {
            hasPagination = true,
            pageNumber = 1,
            pageSize = 3,
            filterGroups = new[] { new { conditions = new[] { new { propertyName = "originCity", value = 7, @operator = 50 } } } }
        });

        Assert.Equal(new[] { target }, result);
    }

    [Fact]
    public async Task Filter_on_status_returns_created_orders()
    {
        var (client, ids) = await ThreeOrdersAsync();

        var result = await GetOrderIdsAsync(client, new
        {
            hasPagination = true,
            pageNumber = 1,
            pageSize = 3,
            filterGroups = new[] { new { conditions = new[] { new { propertyName = "status", value = 0, @operator = 50 } } } }
        });

        Assert.Equal(ids.OrderByDescending(id => id), result);
    }
}
