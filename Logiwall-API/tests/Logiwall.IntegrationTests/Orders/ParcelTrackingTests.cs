using System.Net;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Orders;

// GET api/Shipment/Track: the provider-neutral tracking contract over FakeShippingCarrier.
[Collection("api")]
public class ParcelTrackingTests(SqlServerFixture fixture)
{
    private static string NewCode(string prefix = "BA") => prefix + Random.Shared.NextInt64(100_000_000, 999_999_999) + "IR";

    private async Task<HttpResponseMessage> TrackAsync(string providerCode, string code)
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        await OwnParcelAsync(customerId, code.Trim());
        return await client.GetAsync($"api/Shipment/Track?providerCode={providerCode}&code={Uri.EscapeDataString(code)}");
    }

    /// <summary>Gives the customer an eTipax shipment whose parcel has this tracking code.</summary>
    private async Task OwnParcelAsync(int customerId, string trackingCode)
    {
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await fixture.WithDbAsync(async db =>
        {
            var providerId = await db.ServiceProviders.Where(p => p.Code == "etipax").Select(p => p.Id).SingleAsync();
            var parcel = new OrderParcel { OrderId = orderId, Weight = 1, ParcelValue = 100000, Length = 10, Width = 10, Height = 10 };
            var quote = new OrderInqueryOption { OrderId = orderId, ServiceProviderId = providerId, Choosen = true, FinalAmount = 1000, DelivereDate = "" };
            db.AddRange(parcel, quote);
            await db.SaveChangesAsync();
            db.Shipments.Add(new Shipment { OrderId = orderId, ServiceProviderId = providerId, QuoteId = quote.Id,
                TraceCode = "logiwall-track-" + trackingCode, State = ShipmentRegistrationState.Registered, CreatedAt = DateTimeOffset.UtcNow,
                Parcels = [new ShipmentParcel { OrderParcelId = parcel.Id, TrackingCode = trackingCode }] });
            await db.SaveChangesAsync();
            return true;
        });
    }

    [Fact]
    public async Task Track_returns_sender_receiver_journey_and_payment()
    {
        var code = NewCode();
        var sentAt = new DateTimeOffset(2026, 11, 7, 12, 36, 15, TimeSpan.FromHours(3.5));
        fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Trackings[code] = new(code, "ابطال شده", "اکسپرس", sentAt,
            new("فرستنده تست", "تهران", "تهران"), new("گیرنده تست", "شیراز", "فارس"),
            [new("ثبت", sentAt, "تهران", "شعبه", null), new("ابطال بسته", sentAt.AddMinutes(2), "تهران", "شعبه", "درخواست مشتری")],
            new("پس کرایه", 10395953.65m, 10395953.65m, 0m));

        var response = await TrackAsync("ETipax", $" {code} ");

        response.EnsureSuccessStatusCode();
        var data = (await ApiClient.ReadJsonAsync(response)).GetProperty("data");
        Assert.Equal("etipax", data.GetProperty("providerCode").GetString());
        Assert.False(string.IsNullOrEmpty(data.GetProperty("providerName").GetString()));
        Assert.Equal(code, data.GetProperty("trackingCode").GetString());
        Assert.Equal("فرستنده تست", data.GetProperty("sender").GetProperty("fullName").GetString());
        Assert.Equal("شیراز", data.GetProperty("receiver").GetProperty("city").GetString());
        Assert.Equal(new[] { "ثبت", "ابطال بسته" }, data.GetProperty("journey").EnumerateArray().Select(e => e.GetProperty("title").GetString()));
        Assert.Equal(sentAt, data.GetProperty("registeredAt").GetDateTimeOffset());
        Assert.Equal(10395953.65m, data.GetProperty("payment").GetProperty("totalAmount").GetDecimal());
        Assert.Equal("پس کرایه", data.GetProperty("payment").GetProperty("method").GetString());
    }

    [Fact]
    public async Task Track_unknown_code_is_not_found()
    {
        var response = await TrackAsync("etipax", NewCode());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), "ParcelTrackingNotFound");
    }

    [Fact]
    public async Task Track_code_of_another_customer_is_not_found()
    {
        var code = NewCode();
        var (_, ownerId) = await OrderTestData.RegisteredUserAsync(fixture);
        await OwnParcelAsync(ownerId, code);
        fixture.Factory.Services.GetRequiredService<FakeShippingCarrier>().Trackings[code] = new(code, "ثبت", "اکسپرس", DateTimeOffset.UtcNow,
            new("فرستنده", "تهران", "تهران"), new("گیرنده", "شیراز", "فارس"), [], new("پس کرایه", 1000m, 1000m, 0m));
        var (stranger, _) = await OrderTestData.RegisteredUserAsync(fixture);

        var response = await stranger.GetAsync($"api/Shipment/Track?providerCode=etipax&code={code}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), "ParcelTrackingNotFound");
    }

    [Fact]
    public async Task Track_unknown_provider_is_rejected()
    {
        var response = await TrackAsync("unknown-carrier", NewCode());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), "ShippingProviderUnavailable");
    }

    [Theory]
    [InlineData("ab!cd123")]
    [InlineData("abc")]
    public async Task Track_invalid_code_is_rejected_before_the_carrier(string code)
    {
        var response = await TrackAsync("etipax", code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(PersianAssert.Text("ParcelTrackingCodeInvalid"), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Track_carrier_failure_returns_the_unavailable_message()
    {
        var response = await TrackAsync("etipax", NewCode("FAIL"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.ProviderServiceUnavailable));
    }

    [Fact]
    public async Task Track_requires_sign_in()
    {
        var response = await fixture.CreateApiClient().Anonymous.GetAsync($"api/Shipment/Track?providerCode=etipax&code={NewCode()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
