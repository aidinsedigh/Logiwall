using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Identity;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.People;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Application.Abstractions.Providers.PaymentGateways;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Infrastructure.Providers;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Infrastructure.Services.Global;
using Logiwall.Infrastructure.Services.Identity;
using Logiwall.Infrastructure.Services.OrderItems;
using Logiwall.Infrastructure.Services.People;
using Logiwall.Infrastructure.Services.Tools;

namespace Logiwall.Infrastructure.Services;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        //services.TryAddTransient<IPostalServiceBase, PostalServiceBase>();//todo
        services.TryAddTransient<MyTipaxProvider>();
        services.AddTransient<Func<ProviderType, PostalServiceBase>>(serviceProvider => ProviderType =>
        {
            return ProviderType switch
            {
                ProviderType.MyTipax => serviceProvider.GetService<MyTipaxProvider>(),
                _ => throw new ArgumentException("Invalid Postal Service type", nameof(ProviderType)),
            };
        });
        services.TryAddTransient<IAuthenticationService, AuthenticationService>();
        services.TryAddTransient<IPersonService, PersonService>();
        services.TryAddTransient<ICustomerService, CustomerService>();
        services.TryAddTransient<IOrderService, OrderService>();
        services.TryAddTransient<IPaymentService, PaymentService>();
        services.TryAddTransient<ISMSService, SMSService>();
        services.TryAddTransient<IHttpProvider, HttpProvider>();
        services.TryAddTransient<ISep724Provider, Sep724Provider>();

        services.TryAddScoped<IAppSettingService, AppSettingService>();
        services.TryAddScoped<ICommonService, CommonService>();

        return services;
    }
}
