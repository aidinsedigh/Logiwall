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
    private async Task<(HttpClient Client, int CustomerId)> RegisteredUserAsync()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        var client = api.Authed(await api.LoginWithOtpAsync(phone));
        await ApiClient.RegisterAsync(client, "Strong-Pass-1");

        var customerId = await fixture.WithDbAsync(async db =>
        {
            var userId = await db.Users.Where(u => u.UserName == phone).Select(u => u.Id).SingleAsync();
            return await db.People.Where(p => p.UserId == userId).Select(p => p.Customer.Id).SingleAsync();
        });
        return (client, customerId);
    }

    private Task<int> InsertOrderAsync(int customerId) => fixture.WithDbAsync(async db =>
    {
        var order = new Order
        {
            CustomerId = customerId,
            SenderName = "Sender",
            SenderLastName = "Test",
            SenderPhoneNumber = "09000000000",
            OriginCity = 1,
            OriginLat = "35.6892",
            OriginLng = "51.3890",
            OriginAddress = "Origin address",
            OriginalPlaque = "1",
            OriginalUnit = "1",
            ReceiverName = "Receiver",
            ReceiverLastName = "Test",
            ReceiverPhoneNumber = "09000000001",
            DestinationCity = 1,
            DestinationLat = "35.6892",
            DestinationLng = "51.3890",
            DestinationAddress = "Destination address",
            DestinationPlaque = "2",
            DestinationUnit = "2",
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    });

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
