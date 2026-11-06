using System.Net.Http.Json;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Logiwall.Shared.Enums.Constants;

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

    /// <summary>The eTipax service the front selects before it asks for prices.</summary>
    public static Task<int> ServiceIdAsync(SqlServerFixture fixture) => fixture.WithDbAsync(db =>
        db.ProviderReferenceItems.Where(r => r.Category == "service" && r.ExternalCode == "4").Select(r => r.Id).SingleAsync());

    /// <summary>The front's quote request. <see cref="FakeShippingCarrier"/> answers one option of 1000 rials.</summary>
    public static async Task<HttpResponseMessage> GetOptionsAsync(SqlServerFixture fixture, HttpClient client, int orderId,
        PaymentMethod paymentType = PaymentMethod.Online) =>
        await client.GetAsync($"api/Order/GetOptions?orderId={orderId}&serviceReferenceId={await ServiceIdAsync(fixture)}&paymentType={(int)paymentType}");

    /// <summary>Quotes the order the way the front does, and returns the first provider's first option.</summary>
    public static async Task<int> QuoteAsync(SqlServerFixture fixture, HttpClient client, int orderId)
    {
        var quote = await ApiClient.ReadJsonAsync(await GetOptionsAsync(fixture, client, orderId));
        return quote.GetProperty("data")[0].GetProperty("orderOptions")[0].GetProperty("id").GetInt32();
    }

    public static Task<HttpResponseMessage> SelectAsync(HttpClient client, int orderId, int optionId) =>
        client.PostAsJsonAsync("api/Order/SelectOption", new { orderId, orderInqueryOptionId = optionId });

    /// <summary>The front's pay-step body; <c>pickupOptionId</c> is the id <c>SelectOption</c> returned.</summary>
    public static Task<HttpResponseMessage> PayAsync(HttpClient client, int orderId, int pickupOptionId, string callbackUrl = "https://example.test/cb", int paymentType = 71) =>
        client.PostAsJsonAsync("api/Order/SelectPaymentType", new { paymentType, orderId, pickupOptionId, channel = (string?)null, callbackUrl });

    /// <summary>Quote, select and pay: the order ends with a pending payment.</summary>
    public static async Task PendingPaymentAsync(SqlServerFixture fixture, HttpClient client, int orderId)
    {
        var optionId = await QuoteAsync(fixture, client, orderId);
        (await SelectAsync(client, orderId, optionId)).EnsureSuccessStatusCode();
        (await PayAsync(client, orderId, optionId)).EnsureSuccessStatusCode();
    }

    public static Task<int> InsertOrderAsync(SqlServerFixture fixture, int customerId) => fixture.WithDbAsync(async db =>
    {
        var order = NewOrder(customerId);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    });

    /// <summary>Inserts an order the eTipax quote accepts: eTipax-mapped cities and one parcel with carrier references.</summary>
    public static async Task<int> InsertQuotableOrderAsync(SqlServerFixture fixture, int customerId)
    {
        using (var scope = fixture.Factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IShippingReferenceService>().SynchronizeAsync(CancellationToken.None);
        return await fixture.WithDbAsync(async db =>
        {
            var references = db.ProviderReferenceItems.Where(r => r.ServiceProvider.Code == "etipax" && r.IsActive);
            var cities = db.StaticDataMappers.Where(m => m.ProviderType == ProviderType.ETipax);
            var order = NewOrder(customerId);
            order.OriginCity = (short)await cities.Where(m => m.StaticData!.Type == StaticDataType.OriginCity).Select(m => m.StaticDataId).FirstAsync();
            order.DestinationCity = (short)await cities.Where(m => m.StaticData!.Type == StaticDataType.DestinationCity).Select(m => m.StaticDataId).FirstAsync();
            order.OrderParcels = [new OrderParcel
            {
                ParcelTypeId = ParcelType.Package, Weight = 1, Length = 10, Width = 10, Height = 10, ParcelValue = 100000,
                PackagingReferenceId = await references.Where(r => r.Category == "packaging").Select(r => r.Id).FirstAsync(),
                ContentReferenceId = await references.Where(r => r.Category == "content").Select(r => r.Id).FirstAsync(),
            }];
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        });
    }

    private static Order NewOrder(int customerId) => new()
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
}
