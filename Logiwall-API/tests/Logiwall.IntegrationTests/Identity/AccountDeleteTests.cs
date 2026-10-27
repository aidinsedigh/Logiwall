using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Identity;

// Tests for cycle 017 FR-O1: account delete archives the user and keeps its records.
[Collection("api")]
public class AccountDeleteTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Delete_archives_the_user_and_keeps_orders_and_payments()
    {
        var (client, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var orderId = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var quote = await ApiClient.ReadJsonAsync(await client.GetAsync($"api/Order/GetOptions?orderId={orderId}"));
        var optionId = quote.GetProperty("data")[0].GetProperty("id").GetInt32();
        (await client.PostAsJsonAsync("api/Order/SelectOption",
            new { orderId, orderInqueryOptionId = optionId, channel = "pwa", callbackUrl = "https://example.test/cb" })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync("api/User/Delete");

        response.EnsureSuccessStatusCode();
        var (phone, isArchive, active, orders, payments) = await fixture.WithDbAsync(async db =>
        {
            var userId = await db.People.Where(p => p.Customer.Id == customerId).Select(p => p.UserId).SingleAsync();
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            var orders = await db.Orders.CountAsync(o => o.Id == orderId);
            var payments = await db.OrderPayments.CountAsync(p => p.OrderId == orderId);
            return (user.UserName!, user.IsArchive, user.Active, orders, payments);
        });
        Assert.True(isArchive);
        Assert.False(active);
        Assert.Equal(1, orders);
        Assert.Equal(1, payments);

        var sendOtp = await fixture.CreateApiClient().Anonymous.PostAsJsonAsync("api/Identity/SendOtpCode",
            new { phoneNumber = phone, channel = "pwa", loginProvider = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, sendOtp.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(sendOtp), nameof(AppStrings.AccountIsDisabled));
    }
}
