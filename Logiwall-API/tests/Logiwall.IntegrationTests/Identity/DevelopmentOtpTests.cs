using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Logiwall.Infrastructure.Options;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Logiwall.IntegrationTests.Identity;

[Collection("api")]
public class DevelopmentOtpTests(SqlServerFixture fixture)
{
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
