using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;

namespace Logiwall.Infrastructure.Providers.PostalServices;

/// <summary>A courier. Register each subclass with <c>TryAddEnumerable</c> in <c>DependencyInjection</c>.</summary>
public abstract class PostalServiceBase(IAppSettingService appSettingService,
    IHttpProvider httpProvider)
{
    /// <summary>Selects this carrier's <c>StaticDataMapper</c> codes.</summary>
    public abstract ProviderType ProviderType { get; }

    protected IAppSettingService AppSettingService { get; } = appSettingService;
    protected IHttpProvider HttpProvider { get; } = httpProvider;

    public abstract Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto);
}
