using System.Net.Http.Json;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Services.OrderItems;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>Arranges registered users and orders directly in the database.</summary>
public static class OrderTestData
{
    public static async Task<(HttpClient Client, int CustomerId)> RegisteredUserAsync(SqlServerFixture fixture)
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

    /// <summary>A pickup time <c>SelectOption</c> accepts now.</summary>
    public static string PickupId() => PickupSlots.For(DateTimeOffset.Now)[0].Times[0].Id;

    /// <summary>Quotes the order the way the front does, and returns the first provider's first option.</summary>
    public static async Task<int> QuoteAsync(HttpClient client, int orderId)
    {
        var quote = await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Order/GetOptions?orderId={orderId}"));
        return quote.GetProperty("data")[0].GetProperty("orderOptions")[0].GetProperty("id").GetInt32();
    }

    public static Task<HttpResponseMessage> SelectAsync(HttpClient client, int orderId, int optionId) =>
        client.PostAsJsonAsync("api/Order/SelectOption", new { orderId, orderInqueryOptionId = optionId, pickupId = PickupId() });

    /// <summary>The front's pay-step body; <c>pickupOptionId</c> is the id <c>SelectOption</c> returned.</summary>
    public static Task<HttpResponseMessage> PayAsync(HttpClient client, int orderId, int pickupOptionId, string callbackUrl = "https://example.test/cb", int paymentType = 71) =>
        client.PostAsJsonAsync("api/Order/SelectPaymentType", new { paymentType, orderId, pickupOptionId, channel = (string?)null, callbackUrl });

    /// <summary>Quote, select and pay: the order ends with a pending payment.</summary>
    public static async Task PendingPaymentAsync(HttpClient client, int orderId)
    {
        var optionId = await QuoteAsync(client, orderId);
        (await SelectAsync(client, orderId, optionId)).EnsureSuccessStatusCode();
        (await PayAsync(client, orderId, optionId)).EnsureSuccessStatusCode();
    }

    public static Task<int> InsertOrderAsync(SqlServerFixture fixture, int customerId) => fixture.WithDbAsync(async db =>
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
}
