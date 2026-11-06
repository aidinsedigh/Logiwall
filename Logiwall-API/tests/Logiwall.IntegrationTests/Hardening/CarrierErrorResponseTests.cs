using System.Text.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Server.Services;
using Logiwall.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Hardening;

[Collection("api")]
public class CarrierErrorResponseTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Carrier_error_has_only_message_and_matching_status()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetServices<IExceptionHandler>().OfType<ServerExceptionHandler>().Single();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = new MemoryStream();
        const string message = "وزن بسته برای سرویس انتخاب‌شده مجاز نیست.";
        Assert.True(await handler.TryHandleAsync(context, new CarrierRequestException(message, 422), CancellationToken.None));
        Assert.Equal(422, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(2, body.RootElement.EnumerateObject().Count());
        Assert.Equal(message, body.RootElement.GetProperty("message").GetString());
        Assert.Equal(422, body.RootElement.GetProperty("status").GetInt32());
    }
}
