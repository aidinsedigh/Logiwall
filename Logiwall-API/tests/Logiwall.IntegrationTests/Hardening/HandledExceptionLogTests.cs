using System.Net;
using Logiwall.Application.Abstractions.Global;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Exceptions;
using Logiwall.Shared.Resources;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Events;

namespace Logiwall.IntegrationTests.Hardening;

// Tests for cycle 017 (ADR-0004): ServerExceptionHandler logs each handled exception once.
[Collection("api")]
public class HandledExceptionLogTests(SqlServerFixture fixture)
{
    private sealed class ThrowingCommonService(Exception exception) : ICommonService
    {
        public Task<ServiceResponse<List<GetStaticDataResponseDto>>> GetStaticDatasAsync(GetStaticDataRequestDto requestDto) => throw exception;
    }

    private async Task<(HttpResponseMessage Response, List<LogEvent> Entries)> CallThrowingAsync(Exception exception)
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICommonService>();
            services.AddSingleton<ICommonService>(new ThrowingCommonService(exception));
        }));

        var response = await factory.CreateClient().GetAsync("api/Common/GetStaticDatas");
        return (response, fixture.Factory.LogSink.Events.Where(e => e.Exception == exception).ToList());
    }

    [Fact]
    public async Task Unknown_exception_is_logged_once_as_error_and_answered_in_persian()
    {
        var (response, entries) = await CallThrowingAsync(new InvalidOperationException("boom"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        PersianAssert.HasMessage(await ApiClient.ReadJsonAsync(response), nameof(AppStrings.UnknownException));
        var entry = Assert.Single(entries);
        Assert.Equal(LogEventLevel.Error, entry.Level);
        Assert.StartsWith("[ServerException][Handle]", entry.MessageTemplate.Text);
    }

    [Fact]
    public async Task Known_exception_is_logged_once_as_warning()
    {
        var (response, entries) = await CallThrowingAsync(new ResourceNotFoundException());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var entry = Assert.Single(entries);
        Assert.Equal(LogEventLevel.Warning, entry.Level);
        Assert.StartsWith("[ServerException][Handle]", entry.MessageTemplate.Text);
    }
}
