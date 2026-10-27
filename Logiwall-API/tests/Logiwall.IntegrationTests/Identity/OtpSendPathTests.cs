using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Identity;

// Tests for plan 008: the code is stored before the SMS, and disabled accounts cannot log in.
[Collection("api")]
public class OtpSendPathTests(SqlServerFixture fixture)
{
    private FakeSmsService Sms => fixture.Factory.Services.GetRequiredService<FakeSmsService>();

    private static Task<HttpResponseMessage> SendOtpAsync(HttpClient client, string phone) =>
        client.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = phone, channel = "pwa", loginProvider = 20 });

    [Fact]
    public async Task Sms_failure_returns_400_but_code_is_stored()
    {
        var client = fixture.CreateApiClient().Anonymous;
        var phone = ApiClient.NewPhone();
        Sms.NextResult = "ERROR";
        try
        {
            var response = await SendOtpAsync(client, phone);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.OtpServiceIsNotConnected));
            var otpTokens = await fixture.WithDbAsync(async db =>
            {
                var userId = await db.Users.Where(u => u.UserName == phone).Select(u => u.Id).SingleAsync();
                return await db.UserTokens.CountAsync(t => t.UserId == userId && t.LoginProvider == "OTP" && t.Name == "otp");
            });
            Assert.Equal(1, otpTokens);
        }
        finally
        {
            Sms.NextResult = "OK";
        }
    }

    [Fact]
    public async Task Archived_user_cannot_request_otp()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        await api.LoginWithOtpAsync(phone);
        await fixture.WithDbAsync(db => db.Users.Where(u => u.UserName == phone).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsArchive, true)));
        var smsBefore = Sms.CountFor(phone);

        var response = await SendOtpAsync(api.Anonymous, phone);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.AccountIsDisabled));
        Assert.Equal(smsBefore, Sms.CountFor(phone));
    }

    [Fact]
    public async Task Inactive_user_cannot_get_token()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        var token = await api.LoginWithOtpAsync(phone);
        await ApiClient.RegisterAsync(api.Authed(token), "Strong-Pass-1");
        await fixture.WithDbAsync(db => db.Users.Where(u => u.UserName == phone).ExecuteUpdateAsync(s => s.SetProperty(u => u.Active, false)));

        var response = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Strong-Pass-1", channel = "pwa", loginProvider = 20 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.InvalidUsernameOrPassword));
    }
}
