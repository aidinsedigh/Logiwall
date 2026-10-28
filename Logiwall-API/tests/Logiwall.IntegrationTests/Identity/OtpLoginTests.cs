using System.Net;
using Logiwall.IntegrationTests.Infrastructure;

namespace Logiwall.IntegrationTests.Identity;

[Collection("api")]
public class OtpLoginTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Otp_login_returns_a_bearer_token_that_authenticates()
    {
        var api = fixture.CreateApiClient();
        var token = await api.LoginWithOtpAsync(ApiClient.NewPhone());

        var response = await api.Authed(token).GetAsync("api/Person/GetUserProfile");

        // An unregistered user gets 400 "User is not registered"; what matters is that the token is accepted.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
