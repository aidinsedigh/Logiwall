using System.Collections.Concurrent;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Infrastructure.Options;
using Logiwall.Infrastructure.Providers;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
using Logiwall.Shared.Dtos.Providers.Sep724;
using Microsoft.Extensions.Caching.Memory;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Logiwall.UnitTests.Providers;

// Tests for plan 013: provider tokens are fetched once and then served from ProviderTokenCache.
public class ProviderTokenCachingTests
{
    private sealed class FakeAppSettings : IAppSettingService
    {
        public Task<AppSettingsDto> GetAllSettingsAsync() => Task.FromResult(new AppSettingsDto
        {
            LogiwallShare = "60",
            SepConnectUrl = "https://connect.test",
            SepPayBaseUrl = "https://pay.test",
            MyTipaxBaseUrl = "https://tipax.test",
        });
    }

    /// <summary>Answers each call with a canned response for its response type, and counts calls per type.</summary>
    private sealed class FakeHttpProvider : IHttpProvider
    {
        public ConcurrentDictionary<Type, int> Calls { get; } = new();

        public IEnumerable<(string Key, string? Value)>? DefaultHeaders { get; set; }

        public Task<TResponse?> PostAsync<TRequest, TResponse, TBody>(HttpProviderRequest<TBody, TRequest> request, Func<HttpResponseMessage, Task>? postCallHandler = null, Func<HttpResponseMessage, Task<TResponse?>>? decodeHandler = null, Func<TRequest?, TResponse?, Task<TResponse?>>? failHandler = null, CancellationToken cancellationToken = default)
            where TRequest : IHttpRequest
            where TResponse : IHttpResponse
        {
            Calls.AddOrUpdate(typeof(TResponse), 1, (_, count) => count + 1);
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

    private static ProviderTokenCache NewCache() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Sep_token_fetched_once_for_two_payment_links()
    {
        var http = new FakeHttpProvider();
        var options = MsOptions.Create(new Sep724Options { Username = "u", Password = "p", BasicAuthorization = "b", Terminal = "t" });
        var provider = new Sep724Provider(new FakeAppSettings(), http, options, NewCache());

        await provider.GetPaymentTokenAsync(new PaymentTokenRequestDto { Amount = 1100 });
        await provider.GetPaymentTokenAsync(new PaymentTokenRequestDto { Amount = 1100 });

        Assert.Equal(1, http.CallsFor<GetTokenResponseDto>());
        Assert.Equal(2, http.CallsFor<SepResponseBase<GetPaymentTokenResponseDto>>());
    }

    [Fact]
    public async Task MyTipax_second_quote_makes_only_inquiry_call()
    {
        var http = new FakeHttpProvider();
        var options = MsOptions.Create(new MyTipaxOptions { TokenUsername = "u", TokenPassword = "p", LoginUsername = "l", LoginPassword = "q" });
        var provider = new MyTipaxProvider(new FakeAppSettings(), http, options, NewCache());

        await provider.GetOrderInquiryAsync(new OrderInquiryRequestDto());
        var callsAfterFirst = http.Calls.Values.Sum();
        await provider.GetOrderInquiryAsync(new OrderInquiryRequestDto());

        Assert.Equal(3, callsAfterFirst); // token, login, inquiry
        Assert.Equal(1, http.CallsFor<MyTipaxGetTokenResponseDto>());
        Assert.Equal(1, http.CallsFor<MyTipaxLoginResponseDto>());
        Assert.Equal(2, http.CallsFor<MyTipaxOrderInquiryResponseDto>());
    }
}
