using System.Net;
using System.Net.Http.Json;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Orders;

// Regression tests for plan 002: orders and their options resolve only for their owner.
[Collection("api")]
public class OrderOwnershipTests(SqlServerFixture fixture)
{
    private Task<(HttpClient Client, int CustomerId)> RegisteredUserAsync() => OrderTestData.RegisteredUserAsync(fixture);

    private Task<int> InsertOrderAsync(int customerId) => OrderTestData.InsertOrderAsync(fixture, customerId);

    [Fact]
    public async Task Other_user_cannot_get_options_for_an_order()
    {
        var (_, ownerCustomerId) = await RegisteredUserAsync();
        var (other, _) = await RegisteredUserAsync();
        var orderId = await InsertOrderAsync(ownerCustomerId);

        var response = await other.GetAsync($"api/Order/GetOptions?orderId={orderId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid OrderId", (await ApiClient.ReadJsonAsync(response)).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Option_of_another_order_cannot_be_selected()
    {
        var (owner, customerId) = await RegisteredUserAsync();
        var firstOrderId = await InsertOrderAsync(customerId);
        var secondOrderId = await InsertOrderAsync(customerId);

        var optionId = await fixture.WithDbAsync(async db =>
        {
            var option = new OrderInqueryOption
            {
                OrderId = secondOrderId,
                ServiceProviderId = await db.ServiceProviders.Select(s => s.Id).FirstAsync(),
                DelivereDate = "tomorrow",
                FinalAmount = 1000,
            };
            db.OrderInqueryOptions.Add(option);
            await db.SaveChangesAsync();
            return option.Id;
        });

        var response = await owner.PostAsJsonAsync("api/Order/SelectOption", new
        {
            orderId = firstOrderId,
            orderInqueryOptionId = optionId,
            channel = "pwa",
            callbackUrl = "https://example.test/cb"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid OrderInqueryOptionId", (await ApiClient.ReadJsonAsync(response)).GetProperty("message").GetString());
    }
}
