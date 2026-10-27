using Logiwall.Application.Abstractions.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Logiwall.IntegrationTests.Infrastructure;

public class LogiwallApiFactory : WebApplicationFactory<Logiwall.Server.AppSettings>
{
    private readonly string connectionString;

    public LogiwallApiFactory(string connectionString)
    {
        this.connectionString = connectionString;
        // Program.Services.cs reads the connection string while it builds the host,
        // so the override must also be visible as an environment variable.
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlServerConnectionString", connectionString);

        TestCertificate.WriteTo(AppContext.BaseDirectory);
        Environment.SetEnvironmentVariable("AppSettings__IdentitySettings__IdentityCertificatePassword", TestCertificate.Password);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:SqlServerConnectionString", connectionString);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISMSService>();
            services.AddSingleton<FakeSmsService>();
            services.AddSingleton<ISMSService>(sp => sp.GetRequiredService<FakeSmsService>());
        });
    }
}
