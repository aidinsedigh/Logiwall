using System.Collections.Concurrent;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
using Logiwall.Shared.Dtos.Providers.Sep724;

namespace Logiwall.UnitTests.Providers;

internal sealed class FakeAppSettings : IAppSettingService
{
    public const string LogiwallIban = "IR000000000000000000000001";
    public const string MyTipaxIban = "IR000000000000000000000002";

    public Task<AppSettingsDto> GetAllSettingsAsync() => Task.FromResult(new AppSettingsDto
    {
        LogiwallShare = "60",
        SepConnectUrl = "https://connect.test",
        SepPayBaseUrl = "https://pay.test",
        MyTipaxBaseUrl = "https://tipax.test",
        LogiwallIban = LogiwallIban,
        MyTipaxIban = MyTipaxIban,
    });
}

/// <summary>Answers each call with a canned response for its response type, counts calls per type and keeps the last body.</summary>
internal sealed class FakeHttpProvider : IHttpProvider
{
    public ConcurrentDictionary<Type, int> Calls { get; } = new();

    public object? LastBody { get; private set; }

    public IEnumerable<(string Key, string? Value)>? DefaultHeaders { get; set; }

    public Task<TResponse?> PostAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest
        where TResponse : IHttpResponse
    {
        Calls.AddOrUpdate(typeof(TResponse), 1, (_, count) => count + 1);
        LastBody = request.Body;
        object response = typeof(TResponse) switch
        {
            var t when t == typeof(GetTokenResponseDto) => new GetTokenResponseDto { AccessToken = "sep-access", ExpireIn = 3600 },
            var t when t == typeof(SepResponseBase<GetPaymentTokenResponseDto>) => new SepResponseBase<GetPaymentTokenResponseDto>
            {
                Data = new GetPaymentTokenResponseDto { RequestId = 1, ReferenceCode = Guid.NewGuid(), PaymentLink = "https://pay.test/x" }
            },
            var t when t == typeof(MyTipaxGetTokenResponseDto) => new MyTipaxGetTokenResponseDto { Token = "tipax-api" },
            var t when t == typeof(MyTipaxLoginResponseDto) => new MyTipaxLoginResponseDto { Token = "tipax-login" },
            var t when t == typeof(MyTipaxOrderInquiryResponseDto) => new MyTipaxOrderInquiryResponseDto { Order = new OrderInquiryResponseDto { Options = [] } },
            _ => throw new NotSupportedException(typeof(TResponse).Name),
        };
        return Task.FromResult((TResponse?)response);
    }

    public Task<TResponse?> PutAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest where TResponse : IHttpResponse => throw new NotSupportedException();

    public Task<TResponse?> GetAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest where TResponse : IHttpResponse => throw new NotSupportedException();

    public Task<TResponse?> DeleteAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
        where TRequest : IHttpRequest where TResponse : IHttpResponse => throw new NotSupportedException();

    public int CallsFor<TResponse>() => Calls.TryGetValue(typeof(TResponse), out var count) ? count : 0;
}
