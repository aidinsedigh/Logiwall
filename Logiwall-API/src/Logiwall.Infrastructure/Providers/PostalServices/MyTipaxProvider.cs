using System.Text.Json;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Providers.PostalServices;
public class MyTipaxProvider : PostalServiceBase
{
    private readonly string tokenUrl = "token";
    private readonly string loginUrl = "/api/v1/accounts/login";
    private readonly string inquiryUrl = "/api/v1/orders/inquiry";
    private string token = string.Empty;
    private readonly MyTipaxOptions options;

    public MyTipaxProvider(IAppSettingService appSettingService,
        IHttpProvider httpProvider,
        IOptions<MyTipaxOptions> myTipaxOptions) : base(appSettingService, httpProvider)
    {
        this.options = myTipaxOptions.Value;
    }

    public override async Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto)
    {
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var token = await LoginAsync();
        var header = await GetHeaderAsync();
        var request = new MyTipaxOrderInquiryRequestDto
        {
            Order = requestDto,
            Token = token,
            Version = appSettings.MyTipaxInquiryVersion
        };

        var result = await HttpProvider.PostAsync<MyTipaxOrderInquiryRequestDto, MyTipaxOrderInquiryResponseDto, MyTipaxOrderInquiryRequestDto>
            (new HttpProviderRequest<MyTipaxOrderInquiryRequestDto, MyTipaxOrderInquiryRequestDto>
            {
                BaseAddress = appSettings.MyTipaxBaseUrl,
                Body = request,
                HeaderParameters = header,
                Uri = inquiryUrl,
                Request = request
            });

        return result?.Order?.Options;
    }

    private async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(token))
            return token;
        EnsureCredentialsConfigured(options.TokenUsername, options.TokenPassword);
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var request = new MyTipaxGetTokenRequestDto
        {
            Username = options.TokenUsername,
            Password = options.TokenPassword,
        };
        var response = await HttpProvider.PostAsync<MyTipaxGetTokenRequestDto, MyTipaxGetTokenResponseDto, MyTipaxGetTokenRequestDto>
            (new HttpProviderRequest<MyTipaxGetTokenRequestDto, MyTipaxGetTokenRequestDto>
            {
                Request = request,
                Uri = tokenUrl,
                BaseAddress = appSettings.MyTipaxBaseUrl,
                Body = request,
            });
        token = response?.Token;
        return token;
    }

    private async Task<string?> LoginAsync()
    {
        EnsureCredentialsConfigured(options.LoginUsername, options.LoginPassword);
        var header = await GetHeaderAsync();
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var request = new MyTipaxLoginRequestDto
        {
            Account = new MyTipaxLoginAccountDto
            {
                CellPhoneNumber = options.LoginUsername,
                Password = options.LoginPassword
            },
        };

        var response = await HttpProvider.PostAsync<MyTipaxLoginRequestDto, MyTipaxLoginResponseDto, MyTipaxLoginRequestDto>
            (new HttpProviderRequest<MyTipaxLoginRequestDto, MyTipaxLoginRequestDto>
            {
                Request = request,
                BaseAddress = appSettings.MyTipaxBaseUrl,
                Body = request,
                Uri = loginUrl,
                HeaderParameters = header,
            });

        return response?.Token;
    }

    private async Task<List<(string, string)>> GetHeaderAsync()
    {
        var token = await GetTokenAsync();
        return new List<(string, string)>
        {
            new ("Authorization",token ?? string.Empty)
        };
    }

    private static void EnsureCredentialsConfigured(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Providers:MyTipax credentials are not configured.");
    }
}
