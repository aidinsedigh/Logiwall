using System.Net;
using System.Text;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Server.Services;

namespace Logiwall.IntegrationTests.Hardening;

// Tests for cycle 017 (ADR-0004): one correlation ID per request.
[Collection("api")]
public class CorrelationIdTests(SqlServerFixture fixture)
{
    [Theory]
    [InlineData("test-123")]
    [InlineData("A_b-9")]
    public void Sanitise_keeps_a_safe_value(string value) => Assert.Equal(value, CorrelationIdMiddleware.Sanitise(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("new\nline")]
    [InlineData("ä")]
    public void Sanitise_replaces_an_unsafe_value_with_a_guid(string? value) =>
        Assert.True(Guid.TryParse(CorrelationIdMiddleware.Sanitise(value), out _));

    [Fact]
    public void Sanitise_replaces_a_65_character_value() =>
        Assert.True(Guid.TryParse(CorrelationIdMiddleware.Sanitise(new string('a', 65)), out _));

    [Fact]
    public async Task Request_header_is_returned()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/Common/GetStaticDatas");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "test-123");

        var response = await fixture.CreateApiClient().Anonymous.SendAsync(request);

        Assert.Equal("test-123", Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
    }

    [Fact]
    public async Task Request_without_header_gets_a_guid()
    {
        var response = await fixture.CreateApiClient().Anonymous.GetAsync("api/Common/GetStaticDatas");

        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)), out _));
    }

    [Fact]
    public async Task Failed_request_has_the_same_request_id_and_correlation_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/Identity/SendOtpCode")
        {
            Content = new StringContent("{", Encoding.UTF8, "application/json")
        };
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "failing-request-1");

        var response = await fixture.CreateApiClient().Anonymous.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("failing-request-1", Assert.Single(response.Headers.GetValues("Request-Id")));
        Assert.Equal("failing-request-1", Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
    }
}
