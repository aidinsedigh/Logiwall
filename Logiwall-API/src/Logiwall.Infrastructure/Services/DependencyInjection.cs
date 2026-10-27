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
using Logiwall.Infrastructure.Options;
using Logiwall.Infrastructure.Services.Tools;

namespace Logiwall.Infrastructure.Services;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Sep724Options>(configuration.GetSection(Sep724Options.SectionName));
        services.Configure<MyTipaxOptions>(configuration.GetSection(MyTipaxOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        // The user waits on the OTP request, so the SMS gateway gets a short timeout.
        services.AddHttpClient("sms", c => c.Timeout = TimeSpan.FromSeconds(10));

        // Every carrier is one PostalServiceBase; OrderService queries all of them.
        services.TryAddEnumerable(ServiceDescriptor.Transient<PostalServiceBase, MyTipaxProvider>());
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
