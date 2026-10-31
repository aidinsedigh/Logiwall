using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Resources;

namespace Logiwall.IntegrationTests.People;

// Tests for cycle 017 FR-O6: a national code that is in use gets a clear answer, not HTTP 500.
[Collection("api")]
public class RegisterTests(SqlServerFixture fixture)
{
    private static Task<HttpResponseMessage> RegisterAsync(HttpClient authed, string nationalCode) =>
        authed.PostAsJsonAsync("api/Person/Register", new
        {
            name = "Test",
            lastName = "User",
            nationalCode,
            password = "Strong-Pass-1",
            confirmPassword = "Strong-Pass-1"
        });

    [Fact]
    public async Task Second_registration_with_the_same_national_code_is_rejected()
    {
        var api = fixture.CreateApiClient();
        var nationalCode = RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D10");
        var first = api.Authed(await api.LoginWithOtpAsync(ApiClient.NewPhone()));
        var second = api.Authed(await api.LoginWithOtpAsync(ApiClient.NewPhone()));
        (await RegisterAsync(first, nationalCode)).EnsureSuccessStatusCode();

        var response = await RegisterAsync(second, nationalCode);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.NationalCodeIsTaken));
    }
}
