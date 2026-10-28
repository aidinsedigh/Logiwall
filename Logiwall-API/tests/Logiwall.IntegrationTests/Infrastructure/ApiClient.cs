using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Logiwall.IntegrationTests.Infrastructure;

public class ApiClient(LogiwallApiFactory factory, FakeSmsService sms)
{
    public HttpClient Anonymous { get; } = factory.CreateClient();

    public async Task<string> LoginWithOtpAsync(string phone)
    {
        var send = await Anonymous.PostAsJsonAsync("api/Identity/SendOtpCode",
            new { phoneNumber = phone, channel = "pwa", loginProvider = 20 });
        send.EnsureSuccessStatusCode();

        var code = sms.LastCodeFor(phone);

        var confirm = await Anonymous.PostAsJsonAsync("api/Identity/ConfirmationOtpCode",
            new { phoneNumber = phone, code, channel = "pwa", loginProvider = 20 });
        confirm.EnsureSuccessStatusCode();

        var body = await ReadJsonAsync(confirm);
        return body.GetProperty("data").GetProperty("token").GetString()
            ?? throw new InvalidOperationException("ConfirmationOtpCode returned no token.");
    }

    public HttpClient Authed(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task RegisterAsync(HttpClient authed, string password)
    {
        var response = await authed.PostAsJsonAsync("api/Person/Register", new
        {
            name = "Test",
            lastName = "User",
            // PersonalInfos.NationalCode has a unique index, so every registration needs its own.
            nationalCode = RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D10"),
            password,
            confirmPassword = password
        });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>A random phone number, so tests never collide in the shared database.</summary>
    public static string NewPhone() => "09" + RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D9");

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }
}
