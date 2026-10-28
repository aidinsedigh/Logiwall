using System.Diagnostics;
using System.Net;
using System.Text;
using Logiwall.Infrastructure.Providers;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logiwall.UnitTests.Providers;

// Tests for plan 013: HttpProvider timeouts, cancellation, error bodies and log redaction.
public class HttpProviderTests
{
    public record TestRequest : IHttpRequest;

    public record TestResponse : IHttpResponse
    {
        public string? Value { get; set; }
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => respond(request, cancellationToken);
    }

    private sealed class ListLogger : ILogger<HttpProvider>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private static HttpProvider CreateProvider(HttpMessageHandler handler, ILogger<HttpProvider>? logger = null)
    {
        var factory = new ServiceCollection()
            .AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .Services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        return new HttpProvider(factory, logger ?? new ListLogger());
    }

    private static HttpProviderRequest<object, TestRequest> Request(int? timeout = null, IReadOnlyList<(string Key, string Value)>? headers = null) => new()
    {
        Request = new TestRequest(),
        BaseAddress = "https://provider.test",
        Uri = "api/x",
        Body = new { a = 1 },
        Timeout = timeout,
        HeaderParameters = headers,
    };

    [Fact]
    public async Task Html_502_returns_default_not_throw()
    {
        var provider = CreateProvider(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>Bad gateway</html>", Encoding.UTF8, "text/html")
        })));

        var result = await provider.PostAsync<TestRequest, TestResponse, object>(Request());

        Assert.Null(result);
    }

    [Fact]
    public async Task Timeout_is_in_seconds()
    {
        var provider = CreateProvider(new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<TaskCanceledException>(() => provider.PostAsync<TestRequest, TestResponse, object>(Request(timeout: 1)));

        Assert.True(stopwatch.ElapsedMilliseconds >= 900, $"Timed out after {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        var provider = CreateProvider(new FakeHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.PostAsync<TestRequest, TestResponse, object>(Request(), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Header_values_are_not_logged()
    {
        var logger = new ListLogger();
        var provider = CreateProvider(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"value\":\"ok\"}", Encoding.UTF8, "application/json")
        })), logger);

        var result = await provider.PostAsync<TestRequest, TestResponse, object>(Request(headers: [("Authorization", "Basic secret-value")]));

        Assert.Equal("ok", result?.Value);
        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(logger.Messages, m => m.Contains("secret-value"));
    }
}
