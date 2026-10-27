using Logiwall.Domain.Entities.OrderItems;
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
