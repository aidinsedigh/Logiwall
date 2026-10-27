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
        Assert.Equal(2, (await ApiClient.ReadJsonAsync(second)).GetProperty("data").GetArrayLength());
        Assert.Equal(2, await OptionCountAsync(orderId));
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
        var quote = await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Order/GetOptions?orderId={orderId}"));
        var chosenId = quote.GetProperty("data")[0].GetProperty("id").GetInt32();

        await client.PostAsJsonAsync("api/Order/SelectOption", new
        {
            orderId,
            orderInqueryOptionId = chosenId,
            channel = "pwa",
            callbackUrl = "https://example.test/cb"
        });

        var (options, state, statuses) = await fixture.WithDbAsync(async db =>
        {
            var options = await db.OrderInqueryOptions.Where(t => t.OrderId == orderId).Select(t => new { t.Id, t.Choosen }).ToListAsync();
            var state = await db.Orders.Where(t => t.Id == orderId).Select(t => t.State).SingleAsync();
            var statuses = await db.OrderStatuses.Where(t => t.OrderId == orderId).Select(t => t.Status).ToListAsync();
            return (options, state, statuses);
        });
        Assert.True(options.Single(t => t.Id == chosenId).Choosen);
        Assert.False(options.Single(t => t.Id != chosenId).Choosen);
        // The fake gateway issues a link, so the order moves on to PaymentPending (plan 012).
        Assert.Equal((short)OrderState.PaymentPending, state);
        Assert.Contains((int)OrderState.SelectedServiceProvider, statuses);
    }

    [Fact(Skip = "no policy gap to exercise")]
    public Task Register_rolls_back_person_when_password_reset_fails() => Task.CompletedTask;
}
