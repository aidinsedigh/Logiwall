using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Logiwall.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Orders;

// Tests for plan 011: idempotent quotes, provider isolation and the recorded selection.
[Collection("api")]
public class OrderQuoteFlowTests(SqlServerFixture fixture)
{
    private Task<int> OptionCountAsync(int orderId) =>
        fixture.WithDbAsync(db => db.OrderInqueryOptions.CountAsync(t => t.OrderId == orderId));

    [Fact]
    public async Task GetOptions_twice_returns_same_count_not_double()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);

        (await client.GetAsync($"api/Order/GetOptions?orderId={orderId}")).EnsureSuccessStatusCode();
        var second = await client.GetAsync($"api/Order/GetOptions?orderId={orderId}");

        second.EnsureSuccessStatusCode();
        var provider = Assert.Single((await ApiClient.ReadJsonAsync(second)).GetProperty("data").EnumerateArray());
        Assert.Equal(2, provider.GetProperty("orderOptions").GetArrayLength());
        Assert.Equal(2, await OptionCountAsync(orderId));
    }

    [Fact]
    public async Task GetOptions_returns_the_front_shape_with_parts_that_add_up()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);

        var provider = (await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Order/GetOptions?orderId={orderId}"))).GetProperty("data")[0];

        Assert.True(provider.GetProperty("providerId").GetInt16() > 0);
        Assert.False(string.IsNullOrEmpty(provider.GetProperty("providerName").GetString()));
        var option = provider.GetProperty("orderOptions")[0];
        // FakePostalProvider: shippingCost 900 + specialService 100, tax 100, total 1100.
        Assert.Equal(1000m, option.GetProperty("baseAmount").GetDecimal());
        Assert.Equal(option.GetProperty("totalAmount").GetDecimal(),
            option.GetProperty("baseAmount").GetDecimal() + option.GetProperty("packageAmount").GetDecimal() + option.GetProperty("taxAmount").GetDecimal());
        // The front selects pickupDates[1].times[0] by default.
        var dates = provider.GetProperty("pickupDates");
        Assert.True(dates.GetArrayLength() >= 2);
        Assert.Equal(OrderTestData.PickupId(), dates[0].GetProperty("times")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetOptions_with_failing_provider_keeps_previous_options()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        (await client.GetAsync($"api/Order/GetOptions?orderId={orderId}")).EnsureSuccessStatusCode();

        FakePostalProvider.Fail = true;
        try
        {
            var response = await client.GetAsync($"api/Order/GetOptions?orderId={orderId}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.ProviderServiceUnavailable));
            Assert.Equal(2, await OptionCountAsync(orderId));
        }
        finally
        {
            FakePostalProvider.Fail = false;
        }
    }

    [Fact]
    public async Task SelectOption_marks_choice_and_state()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var chosenId = await OrderTestData.QuoteAsync(client, orderId);

        var response = await OrderTestData.SelectAsync(client, orderId, chosenId);

        response.EnsureSuccessStatusCode();
        var pickupOption = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("pickupOptions")[0];
        Assert.Equal(chosenId, pickupOption.GetProperty("id").GetInt32());
        Assert.Equal(1100m, pickupOption.GetProperty("finalAmount").GetDecimal());
        var (options, state, statuses, pickup, payments) = await fixture.WithDbAsync(async db =>
        {
            var options = await db.OrderInqueryOptions.Where(t => t.OrderId == orderId).Select(t => new { t.Id, t.Choosen }).ToListAsync();
            var order = await db.Orders.Where(t => t.Id == orderId).Select(t => new { t.State, t.PickupDateTime }).SingleAsync();
            var statuses = await db.OrderStatuses.Where(t => t.OrderId == orderId).Select(t => t.Status).ToListAsync();
            var payments = await db.OrderPayments.CountAsync(t => t.OrderId == orderId);
            return (options, order.State, statuses, order.PickupDateTime, payments);
        });
        Assert.True(options.Single(t => t.Id == chosenId).Choosen);
        Assert.False(options.Single(t => t.Id != chosenId).Choosen);
        // Payment is a separate step now (SelectPaymentType).
        Assert.Equal((short)OrderState.SelectedServiceProvider, state);
        Assert.Contains((int)OrderState.SelectedServiceProvider, statuses);
        Assert.NotNull(pickup);
        Assert.Equal(0, payments);
    }

    [Fact]
    public async Task SelectOption_rejects_a_pickup_time_that_is_not_offered()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var optionId = await OrderTestData.QuoteAsync(client, orderId);

        var response = await client.PostAsJsonAsync("api/Order/SelectOption", new { orderId, orderInqueryOptionId = optionId, pickupId = "2000-01-01T09" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.InvalidPickupTime));
    }

    [Fact]
    public async Task Parallel_requotes_leave_one_set_of_options()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.GetAsync($"api/Order/GetOptions?orderId={orderId}")));

        Assert.All(responses, r => r.EnsureSuccessStatusCode());
        Assert.Equal(2, await OptionCountAsync(orderId)); // FakePostalProvider quotes two options
    }

    [Fact]
    public async Task Order_list_shows_the_order_state()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.State, (short)OrderState.PaymentPending)));

        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });

        response.EnsureSuccessStatusCode();
        var order = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray()
            .Single(o => o.GetProperty("id").GetInt32() == orderId);
        Assert.Equal((short)OrderState.PaymentPending, order.GetProperty("status").GetInt16());
        Assert.Equal("در انتظار پرداخت", order.GetProperty("statusName").GetString());
    }

    [Fact(Skip = "no policy gap to exercise")]
    public Task Register_rolls_back_person_when_password_reset_fails() => Task.CompletedTask;
}
