using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Logiwall.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Orders;

// Tests for plan 011: idempotent quotes, provider isolation and the recorded selection.
// The quotes go through the production path: GetOptions -> CarrierQuoteService -> FakeShippingCarrier.
[Collection("api")]
public class OrderQuoteFlowTests(SqlServerFixture fixture)
{
    private Task<int> OptionCountAsync(int orderId) =>
        fixture.WithDbAsync(db => db.OrderInqueryOptions.CountAsync(t => t.OrderId == orderId));

    private async Task<(HttpClient Client, int OrderId)> ArrangeAsync()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        return (client, await OrderTestData.InsertQuotableOrderAsync(fixture, customerId));
    }

    [Fact]
    public async Task GetOptions_twice_returns_same_count_not_double()
    {
        var (client, orderId) = await ArrangeAsync();

        (await OrderTestData.GetOptionsAsync(fixture, client, orderId)).EnsureSuccessStatusCode();
        var second = await OrderTestData.GetOptionsAsync(fixture, client, orderId);

        second.EnsureSuccessStatusCode();
        var provider = Assert.Single((await ApiClient.ReadJsonAsync(second)).GetProperty("data").EnumerateArray());
        Assert.Equal(1, provider.GetProperty("orderOptions").GetArrayLength());
        Assert.Equal(1, await OptionCountAsync(orderId));
    }

    [Fact]
    public async Task GetOptions_returns_the_front_shape_with_parts_that_add_up()
    {
        var (client, orderId) = await ArrangeAsync();

        var provider = (await ApiClient.ReadJsonAsync(await OrderTestData.GetOptionsAsync(fixture, client, orderId))).GetProperty("data")[0];

        Assert.True(provider.GetProperty("providerId").GetInt16() > 0);
        Assert.False(string.IsNullOrEmpty(provider.GetProperty("providerName").GetString()));
        var option = provider.GetProperty("orderOptions")[0];
        // FakeShippingCarrier quotes 1000 rials; the carrier gives no breakdown, so the base is the total.
        Assert.Equal(1000m, option.GetProperty("baseAmount").GetDecimal());
        Assert.Equal(option.GetProperty("totalAmount").GetDecimal(),
            option.GetProperty("baseAmount").GetDecimal() + option.GetProperty("packageAmount").GetDecimal() + option.GetProperty("taxAmount").GetDecimal());
        Assert.False(provider.TryGetProperty("pickupDates", out _));
    }

    [Fact]
    public async Task GetOptions_passes_the_selected_service_and_payment_method_to_the_carrier()
    {
        var (client, orderId) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        var trace = "logiwall-" + await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == orderId).Select(o => o.OrderNumber).SingleAsync());

        (await OrderTestData.GetOptionsAsync(fixture, client, orderId, Constants.PaymentMethod.CashOnDelivery)).EnsureSuccessStatusCode();

        var request = Assert.Single(carrier.QuoteRequests, r => r.TraceCode == trace);
        Assert.Equal(4, request.ServiceId);
        Assert.Equal((int)Constants.PaymentMethod.CashOnDelivery, request.PaymentType);
    }

    [Fact]
    public async Task GetOptions_without_a_service_quotes_every_active_service_and_skips_rejected_ones()
    {
        var (client, orderId) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        var trace = "logiwall-" + await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == orderId).Select(o => o.OrderNumber).SingleAsync());
        carrier.RejectedServices[12] = 0;
        try
        {
            var response = await client.GetAsync($"api/Order/GetOptions?orderId={orderId}&paymentType={(int)Constants.PaymentMethod.CashOnDelivery}");

            response.EnsureSuccessStatusCode();
            Assert.Equal(new[] { 4, 12, 13, 14, 15 }, carrier.QuoteRequests.Where(r => r.TraceCode == trace).Select(r => r.ServiceId).Order());
            var options = Assert.Single((await ApiClient.ReadJsonAsync(response)).GetProperty("data").EnumerateArray())
                .GetProperty("orderOptions").EnumerateArray().ToList();
            Assert.Equal(4, options.Count);
            Assert.All(options, o => Assert.False(string.IsNullOrEmpty(o.GetProperty("serviceTitle").GetString())));
            Assert.Equal(4, options.Select(o => o.GetProperty("serviceTitle").GetString()).Distinct().Count());
            Assert.Equal(4, await OptionCountAsync(orderId));
        }
        finally
        {
            carrier.RejectedServices.TryRemove(12, out _);
        }
    }

    [Theory]
    [InlineData("10395953.649", "10396000")]
    [InlineData("13854062.549", "13855000")]
    [InlineData("10396000", "10396000")]
    public async Task GetOptions_rounds_the_carrier_price_up_to_1000_rials(string carrierPriceText, string expectedText)
    {
        var carrierPrice = decimal.Parse(carrierPriceText, System.Globalization.CultureInfo.InvariantCulture);
        var expected = decimal.Parse(expectedText, System.Globalization.CultureInfo.InvariantCulture);
        var (client, orderId) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.QuoteAmounts["logiwall-" + await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == orderId).Select(o => o.OrderNumber).SingleAsync())] = carrierPrice;

        var option = (await ApiClient.ReadJsonAsync(await OrderTestData.GetOptionsAsync(fixture, client, orderId))).GetProperty("data")[0].GetProperty("orderOptions")[0];

        Assert.Equal(expected, option.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(expected, option.GetProperty("baseAmount").GetDecimal());
        Assert.Equal(expected, await fixture.WithDbAsync(db => db.OrderInqueryOptions.Where(o => o.OrderId == orderId).Select(o => o.FinalAmount).SingleAsync()));
    }

    [Fact]
    public async Task GetOptions_for_a_rejected_selected_service_returns_the_carrier_message()
    {
        var (client, orderId) = await ArrangeAsync();
        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.RejectedServices[4] = 0;
        try
        {
            var response = await OrderTestData.GetOptionsAsync(fixture, client, orderId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("Service does not accept this pack type.", (await ApiClient.ReadJsonAsync(response)).GetProperty("message").GetString());
            Assert.Equal(0, await OptionCountAsync(orderId));
        }
        finally
        {
            carrier.RejectedServices.TryRemove(4, out _);
        }
    }

    [Fact]
    public async Task GetOptions_with_failing_provider_keeps_previous_options()
    {
        var (client, orderId) = await ArrangeAsync();
        (await OrderTestData.GetOptionsAsync(fixture, client, orderId)).EnsureSuccessStatusCode();

        var carrier = fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>();
        carrier.FailQuotes = true;
        try
        {
            var response = await OrderTestData.GetOptionsAsync(fixture, client, orderId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.ProviderServiceUnavailable));
            Assert.Equal(1, await OptionCountAsync(orderId));
        }
        finally
        {
            carrier.FailQuotes = false;
        }
    }

    [Fact]
    public async Task SelectOption_marks_choice_and_state()
    {
        var (client, orderId) = await ArrangeAsync();
        var chosenId = await OrderTestData.QuoteAsync(fixture, client, orderId);

        var response = await OrderTestData.SelectAsync(client, orderId, chosenId);

        response.EnsureSuccessStatusCode();
        var pickupOption = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("pickupOptions")[0];
        Assert.Equal(chosenId, pickupOption.GetProperty("id").GetInt32());
        Assert.Equal(1000m, pickupOption.GetProperty("finalAmount").GetDecimal());
        var (options, state, statuses, pickup, payments) = await fixture.WithDbAsync(async db =>
        {
            var options = await db.OrderInqueryOptions.Where(t => t.OrderId == orderId).Select(t => new { t.Id, t.Choosen }).ToListAsync();
            var order = await db.Orders.Where(t => t.Id == orderId).Select(t => new { t.State, t.PickupDateTime }).SingleAsync();
            var statuses = await db.OrderStatuses.Where(t => t.OrderId == orderId).Select(t => t.Status).ToListAsync();
            var payments = await db.OrderPayments.CountAsync(t => t.OrderId == orderId);
            return (options, order.State, statuses, order.PickupDateTime, payments);
        });
        Assert.True(Assert.Single(options).Choosen);
        // Payment is a separate step now (SelectPaymentType).
        Assert.Equal((short)OrderState.SelectedServiceProvider, state);
        Assert.Contains((int)OrderState.SelectedServiceProvider, statuses);
        Assert.Null(pickup);
        Assert.Equal(0, payments);
    }

    [Fact]
    public async Task SelectOption_accepts_price_confirmation_without_a_pickup_time()
    {
        var (client, orderId) = await ArrangeAsync();
        var optionId = await OrderTestData.QuoteAsync(fixture, client, orderId);

        var response = await client.PostAsJsonAsync("api/Order/SelectOption", new { orderId, orderInqueryOptionId = optionId });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Parallel_requotes_leave_one_set_of_options()
    {
        var (client, orderId) = await ArrangeAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => OrderTestData.GetOptionsAsync(fixture, client, orderId)));

        // A re-quote that finds the order changed during its carrier call is rejected instead of saving stale prices.
        Assert.Contains(responses, r => r.IsSuccessStatusCode);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.Conflict, r.StatusCode.ToString()));
        Assert.Equal(1, await OptionCountAsync(orderId));
    }

    [Fact]
    public async Task Order_list_shows_general_status_instead_of_payment_state()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await fixture.WithDbAsync(db => db.Orders.Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.State, (short)OrderState.PaymentPending)
                .SetProperty(o => o.OverallStatus, OrderStatusCode.Processing)));

        var response = await client.PostAsJsonAsync("api/Order/GetOrders", new { });

        response.EnsureSuccessStatusCode();
        var order = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("result").EnumerateArray()
            .Single(o => o.GetProperty("id").GetInt32() == orderId);
        Assert.Equal((short)OrderStatusCode.Processing, order.GetProperty("status").GetInt16());
        Assert.Equal("در حال پردازش", order.GetProperty("statusName").GetString());
    }

    [Fact(Skip = "no policy gap to exercise")]
    public Task Register_rolls_back_person_when_password_reset_fails() => Task.CompletedTask;
}
