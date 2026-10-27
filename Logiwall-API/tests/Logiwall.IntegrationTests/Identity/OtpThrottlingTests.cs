using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Identity;

// Tests for plan 007: OTP attempt cap, resend cooldown, per-IP limits and phone validation.
[Collection("api")]
public class OtpThrottlingTests(SqlServerFixture fixture)
{
    private static Task<HttpResponseMessage> SendOtpAsync(HttpClient client, string phone) =>
        client.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = phone, channel = "pwa", loginProvider = 20 });

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string phone, string code) =>
        client.PostAsJsonAsync("api/Identity/ConfirmationOtpCode", new { phoneNumber = phone, code, channel = "pwa", loginProvider = 20 });

    private FakeSmsService Sms => fixture.Factory.Services.GetRequiredService<FakeSmsService>();

    private static string WrongCodeFor(string realCode) => realCode == "00000" ? "00001" : "00000";

    [Fact]
    public async Task Sixth_guess_fails_even_with_the_right_code()
    {
        var client = fixture.CreateApiClient().Anonymous;
        var phone = ApiClient.NewPhone();
        (await SendOtpAsync(client, phone)).EnsureSuccessStatusCode();
        var code = Sms.LastCodeFor(phone);

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(client, phone, WrongCodeFor(code))).StatusCode);

        var last = await ConfirmAsync(client, phone, code);
        Assert.Equal(HttpStatusCode.BadRequest, last.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(last), nameof(AppStrings.OtpCodeIsNotCorrect));

        var otpTokens = await fixture.WithDbAsync(async db =>
        {
            var userId = await db.Users.Where(u => u.UserName == phone).Select(u => u.Id).SingleAsync();
            return await db.UserTokens.CountAsync(t => t.UserId == userId && t.LoginProvider == "OTP" && t.Name == "otp");
        });
        Assert.Equal(0, otpTokens);
    }

    [Fact]
    public async Task Correct_code_within_attempts_succeeds_and_resets_counter()
    {
        var client = fixture.CreateApiClient().Anonymous;
        var phone = ApiClient.NewPhone();
        (await SendOtpAsync(client, phone)).EnsureSuccessStatusCode();
        var code = Sms.LastCodeFor(phone);

        await ConfirmAsync(client, phone, WrongCodeFor(code));
        await ConfirmAsync(client, phone, WrongCodeFor(code));
        var response = await ConfirmAsync(client, phone, code);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        var attemptTokens = await fixture.WithDbAsync(async db =>
        {
            var userId = await db.Users.Where(u => u.UserName == phone).Select(u => u.Id).SingleAsync();
            return await db.UserTokens.CountAsync(t => t.UserId == userId && t.LoginProvider == "OTP" && t.Name == "attempts");
        });
        Assert.Equal(0, attemptTokens);
    }

    [Fact]
    public async Task Resend_within_cooldown_is_rejected_without_sms()
    {
        var client = fixture.CreateApiClient().Anonymous;
        var phone = ApiClient.NewPhone();

        (await SendOtpAsync(client, phone)).EnsureSuccessStatusCode();
        var second = await SendOtpAsync(client, phone);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(second), nameof(AppStrings.OtpResendCooldown));
        Assert.Equal(1, Sms.CountFor(phone));
    }

    [Fact]
    public async Task Ip_limit_returns_429()
    {
        await using var strictFactory = new LogiwallApiFactory(fixture.ConnectionString, relaxRateLimits: false);
        var client = strictFactory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
            statuses.Add((await SendOtpAsync(client, ApiClient.NewPhone())).StatusCode);

        Assert.All(statuses.Take(5), s => Assert.NotEqual(HttpStatusCode.TooManyRequests, s));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
    }

    [Fact]
    public async Task Malformed_phone_is_rejected()
    {
        var client = fixture.CreateApiClient().Anonymous;

        var response = await SendOtpAsync(client, "12345");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasPersianMessage(await ApiClient.ReadJsonAsync(response));
        Assert.Equal(0, Sms.CountFor("12345"));
    }

    [Fact]
    public async Task Confirm_for_unknown_phone_gets_the_wrong_code_answer()
    {
        var client = fixture.CreateApiClient().Anonymous;

        var response = await ConfirmAsync(client, ApiClient.NewPhone(), "12345");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadJsonAsync(response);
        Assert.False(body.GetProperty("succuss").GetBoolean());
        PersianAssert.HasMessage(body, nameof(AppStrings.OtpCodeIsNotCorrect));
    }
}
