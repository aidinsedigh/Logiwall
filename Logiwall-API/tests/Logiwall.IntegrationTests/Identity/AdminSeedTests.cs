using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;

namespace Logiwall.IntegrationTests.Identity;

// Tests for cycle 018: AdminSeeder gives the admin panel its first admin (ADR-0005).
[Collection("api")]
public class AdminSeedTests(SqlServerFixture fixture)
{
    /// <summary>Starts a host with the AdminSeed settings, so AdminSeeder runs, and signs in with a password.</summary>
    private async Task<JwtSecurityToken> SeedAndSignInAsync(string phone, string seedPassword, string signInPassword)
    {
        await using var host = fixture.Factory.WithWebHostBuilder(builder => builder
            .UseSetting("AdminSeed:PhoneNumber", phone)
            .UseSetting("AdminSeed:Password", seedPassword));
        var login = await host.CreateClient().PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = signInPassword, channel = "pwa", loginProvider = 20 });
        login.EnsureSuccessStatusCode();
        return new JwtSecurityTokenHandler().ReadJwtToken(
            (await ApiClient.ReadJsonAsync(login)).GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task Seeded_user_signs_in_with_the_admin_role()
    {
        var phone = ApiClient.NewPhone();

        var token = await SeedAndSignInAsync(phone, "Seed-Pass-1", "Seed-Pass-1");

        Assert.Contains(token.Claims, claim => claim is { Type: "role", Value: "Admin" });
    }

    [Fact]
    public async Task Seed_keeps_the_password_of_an_existing_user()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        await ApiClient.RegisterAsync(api.Authed(await api.LoginWithOtpAsync(phone)), "Own-Pass-1");

        var token = await SeedAndSignInAsync(phone, "Seed-Pass-2", "Own-Pass-1");
        var seedPassword = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Seed-Pass-2", channel = "pwa", loginProvider = 20 });

        Assert.Contains(token.Claims, claim => claim is { Type: "role", Value: "Admin" });
        Assert.Equal(HttpStatusCode.Unauthorized, seedPassword.StatusCode);
    }
}
