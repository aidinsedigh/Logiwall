using Logiwall.Infrastructure.Options;
using Logiwall.Infrastructure.Providers;
using Logiwall.Shared.Dtos.Providers;
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
}
