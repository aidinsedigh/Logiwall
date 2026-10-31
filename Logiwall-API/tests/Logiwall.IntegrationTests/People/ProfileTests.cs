using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.People;

// Tests for cycle 019: the API accepts the bodies the front sends (E2E report 2026-10-28).
[Collection("api")]
public class ProfileTests(SqlServerFixture fixture)
{
    private const string Password = "Strong-Pass-1";

    private async Task<(ApiClient Api, HttpClient Client, string Phone)> RegisteredAsync(string? email = null)
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        var client = api.Authed(await api.LoginWithOtpAsync(phone));
        (await client.PostAsJsonAsync("api/Person/Register", new
        {
            name = "Test",
            lastName = "User",
            nationalCode = NewNationalCode(),
            email,
            password = Password,
            confirmPassword = Password
        })).EnsureSuccessStatusCode();
        return (api, client, phone);
    }

    private static string NewNationalCode() => RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D10");

    [Fact]
    public async Task ConfirmationOtpCode_returns_the_fields_the_front_reads()
    {
        var api = fixture.CreateApiClient();
        var phone = ApiClient.NewPhone();
        (await api.Anonymous.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = phone, channel = "pwa", loginProvider = 20 })).EnsureSuccessStatusCode();

        var response = await api.Anonymous.PostAsJsonAsync("api/Identity/ConfirmationOtpCode",
            new { phoneNumber = phone, code = fixture.Factory.Services.GetRequiredService<FakeSmsService>().LastCodeFor(phone), channel = "pwa", loginProvider = 20 });

        response.EnsureSuccessStatusCode();
        var body = await ApiClient.ReadJsonAsync(response);
        PersianAssert.HasMessage(body, nameof(AppStrings.OtpLoginSucceeded));
        var data = body.GetProperty("data");
        Assert.False(string.IsNullOrEmpty(data.GetProperty("accessToken").GetString()));
        Assert.True(data.GetProperty("expiresIn").GetInt64() > 0);
    }

    [Fact]
    public async Task Register_email_shows_in_the_profile()
    {
        var (_, client, _) = await RegisteredAsync("test@example.test");

        var profile = (await ApiClient.ReadJsonAsync(await client.GetAsync("api/Person/GetUserProfile"))).GetProperty("data");

        Assert.Equal("test@example.test", profile.GetProperty("email").GetString());
    }

    [Fact]
    public async Task UpdateUserProfile_with_the_front_body_keeps_phone_and_kind()
    {
        var (_, client, phone) = await RegisteredAsync();
        var nationalCode = NewNationalCode();

        // The profile form sends only these fields (Logiwall-Front change-user-info.ts).
        var response = await client.PostAsJsonAsync("api/Person/UpdateUserProfile",
            new { firstName = "New", lastName = "Name", nationalCode, email = "new@example.test", gender = 3 });

        response.EnsureSuccessStatusCode();
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.ProfileIsUpdated));
        var profile = (await ApiClient.ReadJsonAsync(await client.GetAsync("api/Person/GetUserProfile"))).GetProperty("data");
        Assert.Equal(phone, profile.GetProperty("phoneNumber").GetString());
        Assert.Equal("New", profile.GetProperty("firstName").GetString());
        Assert.Equal(nationalCode, profile.GetProperty("nationalCode").GetString());
        Assert.Equal("new@example.test", profile.GetProperty("email").GetString());
        Assert.Equal(1, profile.GetProperty("kind").GetInt32()); // PersonKind.Private from Register
    }

    [Fact]
    public async Task ChangePassword_with_the_right_old_password_changes_it()
    {
        var (api, client, phone) = await RegisteredAsync();

        var response = await client.PostAsJsonAsync("api/Person/ChangePassword", new { oldPassword = Password, newPassword = "Other-Pass-2" });

        response.EnsureSuccessStatusCode();
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.PasswordChangedSuccessfullyMessage));
        var login = await api.Anonymous.PostAsJsonAsync("api/Identity/GetToken",
            new { userName = phone, password = "Other-Pass-2", channel = "pwa", loginProvider = 20 });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_with_a_wrong_old_password_is_rejected_in_persian()
    {
        var (_, client, phone) = await RegisteredAsync();
        var hashBefore = await PasswordHashAsync(phone);

        var response = await client.PostAsJsonAsync("api/Person/ChangePassword", new { oldPassword = "Wrong-Pass-9", newPassword = "Other-Pass-2" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasPersianMessage(await ApiClient.ReadJsonAsync(response));
        Assert.Equal(hashBefore, await PasswordHashAsync(phone));
    }

    [Fact]
    public async Task CreateOrder_rejects_an_unknown_parcel_type()
    {
        var (_, client, _) = await RegisteredAsync();

        var response = await client.PostAsJsonAsync("api/Order/CreateOrder", new
        {
            channelId = 1,
            senderName = "S", senderLastName = "T", senderPhoneNumber = "09000000000",
            originCity = 1, originLat = "35.6", originLng = "51.3", originAddress = "A", originalPlaque = "1", originalUnit = "1",
            receiverName = "R", receiverLastName = "T", receiverPhoneNumber = "09000000001",
            destinationCity = 1, destinationLat = "35.6", destinationLng = "51.3", destinationAddress = "B", destinationPlaque = "2", destinationUnit = "2",
            orderParcels = new[] { new { parcelTypeId = 1, packageTypeId = 0, parcelValue = 5_000_000, length = 10, width = 10, height = 10, weight = 1, cod = 0 } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<string?> PasswordHashAsync(string phone) =>
        fixture.WithDbAsync(db => db.Users.Where(u => u.UserName == phone).Select(u => u.PasswordHash).SingleAsync());
}
