using Logiwall.Infrastructure.Options;
using Logiwall.Infrastructure.Providers;
using Logiwall.Shared.Dtos.Providers.Sep724;
using Microsoft.Extensions.Caching.Memory;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Logiwall.UnitTests.Providers;

// Test for cycle 017 FR-O8: the SEP payment is split between Logiwall and MyTipax by LogiwallShare.
public class Sep724SplitTests
{
    [Theory]
    [InlineData(1100)]
    [InlineData(1001)] // 60% of 1001 is not whole: the remainder goes to MyTipax
    public async Task Split_adds_up_to_the_total_and_logiwall_gets_its_share(long total)
    {
        var http = new FakeHttpProvider();
        var options = MsOptions.Create(new Sep724Options { Username = "u", Password = "p", BasicAuthorization = "b", Terminal = "t" });
        var provider = new Sep724Provider(new FakeAppSettings(), http, options, new ProviderTokenCache(new MemoryCache(new MemoryCacheOptions())));

        await provider.GetPaymentTokenAsync(new PaymentTokenRequestDto { Amount = total });

        var accounts = Assert.IsType<GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>>(http.LastBody).Extensions!.Accounts!;
        var logiwall = Assert.Single(accounts, a => a.Iban == FakeAppSettings.LogiwallIban);
        var myTipax = Assert.Single(accounts, a => a.Iban == FakeAppSettings.MyTipaxIban);
        Assert.Equal(total, logiwall.Amount + myTipax.Amount);
        Assert.Equal(total * 60 / 100, logiwall.Amount); // FakeAppSettings.LogiwallShare = 60
    }
}
