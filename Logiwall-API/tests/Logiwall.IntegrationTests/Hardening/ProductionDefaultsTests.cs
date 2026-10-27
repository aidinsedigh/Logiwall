using System.Net;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Logiwall.IntegrationTests.Hardening;

// Tests for plan 009: template defaults that must be off in production.
[Collection("api")]
public class ProductionDefaultsTests(SqlServerFixture fixture)
{
    private WebApplicationFactory<Logiwall.Server.AppSettings> WithSetting(string key, string value) =>
        fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

    [Fact]
    public async Task Sample_endpoint_is_gone()
    {
        var response = await fixture.CreateApiClient().Anonymous.GetAsync("api/minimal-api-sample/x?queryStringParameter=y");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_is_disabled_when_flag_is_false()
    {
        await using var factory = WithSetting("Swagger:Enabled", "false");

        var response = await factory.CreateClient().GetAsync("swagger/v1/swagger.json");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignUp_endpoint_is_gone()
    {
        var response = await fixture.CreateApiClient().Anonymous.PostAsync("api/Identity/SignUp",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    [Fact]
    public async Task Configured_cors_origin_is_echoed_and_others_are_not()
    {
        await using var factory = WithSetting("Cors:AllowedOrigins:0", "https://app.example.test");
        var client = factory.CreateClient();

        static HttpRequestMessage Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "api/Common/GetStaticDatas");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            return request;
        }

        var allowed = await client.SendAsync(Preflight("https://app.example.test"));
        var denied = await client.SendAsync(Preflight("https://evil.example.test"));

        Assert.True(allowed.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("https://app.example.test", Assert.Single(origins));
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
