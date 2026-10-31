using System.Net;
using System.Net.Http.Json;
using System.Text;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;

namespace Logiwall.IntegrationTests.Hardening;

// Tests for cycle 017 (ADR-0003): every request is answered in Persian.
[Collection("api")]
public class PersianCultureTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task English_accept_language_still_gets_a_persian_message()
    {
        var client = fixture.CreateApiClient().Anonymous;
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Identity/GetToken")
        {
            Content = JsonContent.Create(new { userName = ApiClient.NewPhone(), password = "Wrong-Pass-9", channel = "pwa", loginProvider = 20 })
        };
        request.Headers.Add("Accept-Language", "en-US");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        PersianAssert.HasPersianMessage(await ApiClient.ReadJsonAsync(response));
    }

    [Fact]
    public async Task Malformed_json_gets_persian_messages_without_framework_text()
    {
        var client = fixture.CreateApiClient().Anonymous;

        var response = await client.PostAsync("api/Identity/SendOtpCode",
            new StringContent("{\"phoneNumber\": 09", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiClient.ReadJsonAsync(response);
        PersianAssert.AllMessagesArePersian(body);
        Assert.Contains(PersianAssert.Text(nameof(AppStrings.InvalidRequestFormat)), PersianAssert.Strings(body));
    }

    [Fact]
    public async Task Missing_and_invalid_fields_get_persian_messages()
    {
        var client = fixture.CreateApiClient().Anonymous;

        // No phone number; a channel longer than its StringLength, whose attribute has no resource key.
        var response = await client.PostAsJsonAsync("api/Identity/SendOtpCode", new { channel = "too-long-channel", loginProvider = 20 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.AllMessagesArePersian(await ApiClient.ReadJsonAsync(response));
    }

    [Fact]
    public async Task Rate_limited_request_gets_a_persian_body()
    {
        await using var strictFactory = new LogiwallApiFactory(fixture.ConnectionString, relaxRateLimits: false);
        var client = strictFactory.CreateClient();

        HttpResponseMessage response = null!;
        for (var i = 0; i < 6; i++) // the otp-send limit is 5 per minute
            response = await client.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = ApiClient.NewPhone(), channel = "pwa", loginProvider = 20 });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = await ApiClient.ReadJsonAsync(response);
        PersianAssert.HasMessage(body, nameof(AppStrings.TooManyRequestsExceptions));
        Assert.Equal(nameof(AppStrings.TooManyRequestsExceptions), body.GetProperty("key").GetString());
    }
}
