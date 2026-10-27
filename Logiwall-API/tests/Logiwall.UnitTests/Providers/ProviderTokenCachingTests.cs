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
