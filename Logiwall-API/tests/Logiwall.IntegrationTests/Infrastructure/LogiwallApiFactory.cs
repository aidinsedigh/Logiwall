using Logiwall.Application.Abstractions.Providers;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Infrastructure.Providers.PostalServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace Logiwall.IntegrationTests.Infrastructure;

public class LogiwallApiFactory : WebApplicationFactory<Logiwall.Server.AppSettings>
{
    private readonly string connectionString;
    private readonly bool relaxRateLimits;

    /// <summary>The log events of this host and of the hosts made from it with <c>WithWebHostBuilder</c>.</summary>
    public CapturingLogSink LogSink { get; } = new();

    /// <param name="relaxRateLimits">Tests share one IP address, so most tests lift the per-IP limits.</param>
    public LogiwallApiFactory(string connectionString, bool relaxRateLimits = true)
    {
        this.connectionString = connectionString;
        this.relaxRateLimits = relaxRateLimits;
        // Program.Services.cs reads the connection string while it builds the host,
        // so the override must also be visible as an environment variable.
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlServerConnectionString", connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Development MapStaticAssets adds a GET-only {**path:file} fallback, so unknown POSTs get 405. Production has no such route.
        builder.UseSetting("ReloadStaticAssetsAtRuntime", "false");
        builder.UseSetting("ConnectionStrings:SqlServerConnectionString", connectionString);
        if (relaxRateLimits)
        {
            builder.UseSetting("RateLimits:OtpSendPerMinute", "1000");
            builder.UseSetting("RateLimits:AuthVerifyPerMinute", "1000");
        }
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(LogSink);
            services.RemoveAll<ISMSService>();
            services.AddSingleton<FakeSmsService>();
            services.AddSingleton<ISMSService>(sp => sp.GetRequiredService<FakeSmsService>());
            services.RemoveAll<ISep724Provider>();
            services.AddSingleton<FakeSep724Provider>();
            services.AddSingleton<ISep724Provider>(sp => sp.GetRequiredService<FakeSep724Provider>());
            services.RemoveAll<PostalServiceBase>();
            services.AddTransient<PostalServiceBase, FakePostalProvider>();
        });
    }
}
