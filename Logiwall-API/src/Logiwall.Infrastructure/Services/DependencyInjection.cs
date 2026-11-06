using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Identity;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.People;
using Logiwall.Application.Abstractions.Providers;
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
        services.Configure<ETipaxOptions>(configuration.GetSection(ETipaxOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection("Otp"));
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddHostedService<AdminSeeder>();
        // The user waits on the OTP request, so the SMS gateway gets a short timeout.
        services.AddHttpClient("sms", c => c.Timeout = TimeSpan.FromSeconds(10));

        services.AddMemoryCache();
        services.TryAddSingleton<ProviderTokenCache>();

        // Active carriers use IShippingCarrier; legacy MyTipax is not registered.
        services.AddHttpClient("etipax", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ETipaxOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.TryAddEnumerable(ServiceDescriptor.Transient<IShippingCarrier, ETipaxCarrier>());
        services.TryAddScoped<ICarrierQuoteService, CarrierQuoteService>();
        services.TryAddScoped<IShippingReferenceService, ShippingReferenceService>();
        services.TryAddScoped<ShipmentService>();
        services.TryAddScoped<IShipmentService>(sp => sp.GetRequiredService<ShipmentService>());
        services.AddHostedService<ShippingWorker>();
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
