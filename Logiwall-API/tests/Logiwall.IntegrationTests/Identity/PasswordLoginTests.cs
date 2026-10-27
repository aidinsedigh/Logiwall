using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.Identity;

// Regression tests for plan 001: OTP-created users have no password.
[Collection("api")]
public class PasswordLoginTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Otp_created_user_has_no_password_hash()
    {
        var phone = ApiClient.NewPhone();
        await fixture.CreateApiClient().LoginWithOtpAsync(phone);

        var passwordHash = await fixture.WithDbAsync(db =>
            db.Users.Where(u => u.UserName == phone).Select(u => u.PasswordHash).SingleAsync());

        Assert.Null(passwordHash);
    }

    [Fact]
    public async Task Otp_created_user_cannot_get_token_with_any_password()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        await api.LoginWithOtpAsync(phone);

        var response = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Wrong-Pass-9", channel = "pwa", loginProvider = 20 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registered_user_can_get_token_with_their_password()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        var token = await api.LoginWithOtpAsync(phone);
        await ApiClient.RegisterAsync(api.Authed(token), "Strong-Pass-1");

        var response = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Strong-Pass-1", channel = "pwa", loginProvider = 20 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("accessToken", await response.Content.ReadAsStringAsync());
    }
}
