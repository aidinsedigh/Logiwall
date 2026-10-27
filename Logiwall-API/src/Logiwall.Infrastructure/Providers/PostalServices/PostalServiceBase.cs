using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;

namespace Logiwall.Infrastructure.Providers.PostalServices;

/// <summary>A courier. Register each subclass with <c>TryAddEnumerable</c> in <c>DependencyInjection</c>.</summary>
public abstract class PostalServiceBase
{
    /// <summary>Selects this carrier's <c>StaticDataMapper</c> codes.</summary>
    public abstract ProviderType ProviderType { get; }

    protected PostalServiceBase(IAppSettingService appSettingService,
        IHttpProvider httpProvider)
    {
        AppSettingService = appSettingService;
        HttpProvider = httpProvider;
    }

    protected IAppSettingService AppSettingService { get; }
    protected IHttpProvider HttpProvider { get; }

    public abstract Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto);
}
