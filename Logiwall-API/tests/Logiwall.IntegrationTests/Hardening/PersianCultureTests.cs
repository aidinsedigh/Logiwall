using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;

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
}
