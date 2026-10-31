using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Logiwall.Shared.Enums;

namespace Logiwall.IntegrationTests.Orders;

// Tests for cycle 018: the Blazor client is the admin panel (ADR-0005). Only admins see the
// business-wide order counts.
[Collection("api")]
public class DashboardStatsTests(SqlServerFixture fixture)
{
    private const string StatsUrl = "api/Dashboard/GetOverallAnalyticsStatsData";

    private static async Task<(int Total, int Last30Days, int Paid)> GetStatsAsync(HttpClient client)
    {
        var response = await client.GetAsync(StatsUrl);
        response.EnsureSuccessStatusCode();
        var body = await ApiClient.ReadJsonAsync(response);
        return (body.GetProperty("totalOrders").GetInt32(),
            body.GetProperty("last30DaysOrders").GetInt32(),
            body.GetProperty("paidOrders").GetInt32());
    }

    /// <summary>A registered user with the Admin role, signed in with a password like the Blazor panel.</summary>
    private async Task<string> AdminTokenAsync()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        await ApiClient.RegisterAsync(api.Authed(await api.LoginWithOtpAsync(phone)), "Strong-Pass-1");

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            // The SeedAdminRole migration creates the role.
            var result = await users.AddToRoleAsync((await users.FindByNameAsync(phone))!, "Admin");
            Assert.True(result.Succeeded);
        }

        var login = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Strong-Pass-1", channel = "pwa", loginProvider = 20 });
        login.EnsureSuccessStatusCode();
        return (await ApiClient.ReadJsonAsync(login)).GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task Admin_sees_the_counts_of_all_orders()
    {
        var admin = fixture.CreateApiClient().Authed(await AdminTokenAsync());
        // Other tests add orders to the shared database, so compare the change, not the total.
        var before = await GetStatsAsync(admin);

        var (_, customerId) = await OrderTestData.RegisteredUserAsync(fixture);
        var oldOrder = await OrderTestData.InsertOrderAsync(fixture, customerId);
        var paidOrder = await OrderTestData.InsertOrderAsync(fixture, customerId);
        await OrderTestData.InsertOrderAsync(fixture, customerId);

        var (_, otherCustomerId) = await OrderTestData.RegisteredUserAsync(fixture);
        await OrderTestData.InsertOrderAsync(fixture, otherCustomerId);

        await fixture.WithDbAsync(async db =>
        {
            await db.Orders.Where(o => o.Id == oldOrder)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.CreationDate, DateTimeOffset.Now.AddDays(-40)));
            db.OrderPayments.Add(new OrderPayment
            {
                OrderId = paidOrder,
                Amount = 1000,
                Status = PaymentStatus.Success,
                PayTrackingCode = "1",
                PayDate = DateTimeOffset.Now,
            });
            return await db.SaveChangesAsync();
        });

        var after = await GetStatsAsync(admin);
        Assert.Equal((4, 3, 1), (after.Total - before.Total, after.Last30Days - before.Last30Days, after.Paid - before.Paid));
    }

    [Fact]
    public async Task Admin_token_has_the_role_claim_the_blazor_client_reads()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(await AdminTokenAsync());

        Assert.Contains(token.Claims, claim => claim is { Type: "role", Value: "Admin" });
    }

    [Fact]
    public async Task Customer_gets_403()
    {
        var (customer, _) = await OrderTestData.RegisteredUserAsync(fixture);

        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(StatsUrl)).StatusCode);
    }

    [Fact]
    public async Task Stats_need_a_signed_in_user()
    {
        var response = await fixture.CreateApiClient().Anonymous.GetAsync(StatsUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
