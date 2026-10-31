using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Enums;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>A courier that always quotes two options, or throws while <see cref="Fail"/> is set.</summary>
public class FakePostalProvider(IAppSettingService appSettingService, IHttpProvider httpProvider)
    : PostalServiceBase(appSettingService, httpProvider)
{
    public static volatile bool Fail;

    public override ProviderType ProviderType => ProviderType.MyTipax;

    public override Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto)
    {
        if (Fail)
            throw new HttpRequestException("Fake courier is down.");

        return Task.FromResult(new List<OrderInquiryOptionDto>
        {
            Option("fake-1"),
            Option("fake-2"),
        });
    }

    private static OrderInquiryOptionDto Option(string id) => new()
    {
        ProviderId = id,
        ShippingCost = "900",
        SpecialService = "100",
        PackageService = "0",
        Tax = "100",
        FinalAmount = "1100",
        DelivereDate = "1",
    };
}
