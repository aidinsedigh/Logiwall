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
        var orderId = await OrderTestData.InsertQuotableOrderAsync(fixture, customerId);
        (await OrderTestData.SelectAsync(client, orderId, await OrderTestData.QuoteAsync(fixture, client, orderId))).EnsureSuccessStatusCode();
        var (originName, destinationName) = await fixture.WithDbAsync(async db =>
        {
            var cities = await db.Orders.Where(o => o.Id == orderId).Select(o => new { o.OriginCity, o.DestinationCity }).SingleAsync();
            return (await db.StaticDatas.Where(s => s.Id == cities.OriginCity).Select(s => s.Title).SingleAsync(),
                await db.StaticDatas.Where(s => s.Id == cities.DestinationCity).Select(s => s.Title).SingleAsync());
        });

        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });

        var order = Assert.Single((await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray());
        Assert.Equal(1000m, order.GetProperty("finalAmount").GetDecimal()); // FakeShippingCarrier's total
        Assert.Equal(originName, order.GetProperty("originCityName").GetString());
        Assert.Equal(destinationName, order.GetProperty("destinationCityName").GetString());
        var providerTitle = await fixture.WithDbAsync(db => db.ServiceProviders
            .Where(p => p.Id == order.GetProperty("serviceProviderId").GetInt16()).Select(p => p.Title).SingleAsync());
        Assert.NotNull(providerTitle);
        Assert.Equal(providerTitle, order.GetProperty("serviceProviderTitle").GetString());
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
    public async Task Default_order_uses_creation_date_and_id_for_equal_dates_across_pages()
    {
        var (client, ids) = await ThreeOrdersAsync();
        var now = DateTimeOffset.UtcNow;
        await fixture.WithDbAsync(async db =>
        {
            await db.Orders.Where(o => o.Id == ids[0] || o.Id == ids[1]).ExecuteUpdateAsync(s => s.SetProperty(o => o.CreationDate, now));
            await db.Orders.Where(o => o.Id == ids[2]).ExecuteUpdateAsync(s => s.SetProperty(o => o.CreationDate, now.AddDays(-1)));
            return true;
        });
        Assert.Equal(new[] { ids[1], ids[0], ids[2] }, await GetOrderIdsAsync(client, new { }));
        var first = await GetOrderIdsAsync(client, new { hasPagination = true, pageNumber = 1, pageSize = 2 });
        var second = await GetOrderIdsAsync(client, new { hasPagination = true, pageNumber = 2, pageSize = 2 });
        Assert.Equal(new[] { ids[1], ids[0], ids[2] }, first.Concat(second));
    }

    [Fact]
    public async Task Paid_amount_counts_only_successful_verified_payments()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var id = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await fixture.WithDbAsync(async db =>
        {
            db.OrderPayments.AddRange(
                new() { OrderId = id, Amount = 9999, VerifiedAmount = 1000, VerifiedAt = DateTimeOffset.UtcNow,
                    Status = Logiwall.Shared.Enums.PaymentStatus.Success, PayTrackingCode = "verified", PayDate = DateTimeOffset.UtcNow },
                new() { OrderId = id, Amount = 500, Status = Logiwall.Shared.Enums.PaymentStatus.Pending,
                    PayTrackingCode = "pending", PayDate = DateTimeOffset.UtcNow },
                new() { OrderId = id, Amount = 300, VerifiedAmount = 300, Status = Logiwall.Shared.Enums.PaymentStatus.Success,
                    PayTrackingCode = "unverified", PayDate = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(); return true;
        });
        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });
        response.EnsureSuccessStatusCode();
        var order = Assert.Single((await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray());
        Assert.Equal(1000, order.GetProperty("paidAmount").GetDecimal());
        Assert.Equal("Paid", order.GetProperty("paymentStatus").GetString());
        Assert.Equal(0, order.GetProperty("finalAmount").GetDecimal());
    }

    [Fact]
    public async Task A_carrier_settlement_method_is_not_reported_as_a_successful_payment()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var id = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ServiceProviders.Where(p => p.Code == "etipax").Select(p => p.Id).SingleAsync();
            db.OrderInqueryOptions.Add(new() { OrderId = id, ServiceProviderId = providerId, Choosen = true,
                PaymentMethod = Logiwall.Shared.Enums.Constants.PaymentMethod.CashOnDelivery, FinalAmount = 1000 });
            await db.SaveChangesAsync(); return true;
        });
        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });
        response.EnsureSuccessStatusCode();
        var order = Assert.Single((await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray());
        Assert.Equal("Unpaid", order.GetProperty("paymentStatus").GetString());
        Assert.Equal(0, order.GetProperty("paidAmount").GetDecimal());
        Assert.Equal(1000, order.GetProperty("finalAmount").GetDecimal());
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
    public async Task Status_filter_uses_the_general_catalogue_id_instead_of_payment_state_or_title()
    {
        var (client, ids) = await ThreeOrdersAsync();
        await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == ids[1]).ExecuteUpdateAsync(s =>
            s.SetProperty(o => o.OverallStatus, Logiwall.Shared.Enums.OrderStatusCode.Delivered)
                .SetProperty(o => o.State, (short)Logiwall.Shared.Enums.OrderState.PaymentPending)));
        var result = await GetOrderIdsAsync(client, new
        {
            hasPagination = true, pageNumber = 1, pageSize = 10,
            filterGroups = new[] { new { logicalOperator = 60, conditions = new[] { new { propertyName = "status", value = 20, @operator = 50 } } } }
        });
        Assert.Equal(new[] { ids[1] }, result);
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
