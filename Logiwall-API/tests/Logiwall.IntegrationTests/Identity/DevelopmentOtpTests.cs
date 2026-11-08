using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Infrastructure.Options;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Logiwall.IntegrationTests.Identity;

[Collection("api")]
public class DevelopmentOtpTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Explicit_fixed_code_works_in_production_without_sending_sms()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IHostEnvironment>(new Microsoft.Extensions.Hosting.Internal.HostingEnvironment
                { EnvironmentName = Environments.Production });
            services.PostConfigure<OtpOptions>(options =>
            {
                options.UseDevelopmentCode = false;
                options.UseFixedCode = true;
            });
        }));
        using var client = factory.CreateClient();
        var sms = factory.Services.GetRequiredService<FakeSmsService>();
        sms.NextResult = "ERROR";
        var phone = ApiClient.NewPhone();
        (await client.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = phone, channel = "pwa", loginProvider = 20 })).EnsureSuccessStatusCode();
        Assert.Equal(0, sms.CountFor(phone));
        var wrong = await client.PostAsJsonAsync("api/Identity/ConfirmationOtpCode", new { phoneNumber = phone, code = "54321", channel = "pwa", loginProvider = 20 });
        Assert.False(wrong.IsSuccessStatusCode);
        var response = await client.PostAsJsonAsync("api/Identity/ConfirmationOtpCode", new { phoneNumber = phone, code = "12345", channel = "pwa", loginProvider = 20 });
        response.EnsureSuccessStatusCode();
        Assert.False(string.IsNullOrEmpty((await ApiClient.ReadJsonAsync(response)).GetProperty("data").GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Development_code_logs_in_without_sending_sms()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<OtpOptions>(options => options.UseDevelopmentCode = true)));
        using var client = factory.CreateClient();
        var sms = factory.Services.GetRequiredService<FakeSmsService>();
        sms.NextResult = "ERROR";
        try
        {
            var phone = ApiClient.NewPhone();
            (await client.PostAsJsonAsync("api/Identity/SendOtpCode", new { phoneNumber = phone, channel = "pwa", loginProvider = 20 })).EnsureSuccessStatusCode();
            var response = await client.PostAsJsonAsync("api/Identity/ConfirmationOtpCode", new
            { phoneNumber = phone, code = "12345", channel = "pwa", loginProvider = 20 });
            response.EnsureSuccessStatusCode();
            var json = await ApiClient.ReadJsonAsync(response);
            Assert.False(string.IsNullOrEmpty(json.GetProperty("data").GetProperty("accessToken").GetString()));
        }
        finally { sms.NextResult = "OK"; }
    }
}
