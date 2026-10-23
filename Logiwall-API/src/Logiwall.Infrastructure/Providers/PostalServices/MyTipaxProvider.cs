using System.Text.Json;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Dtos.Providers.PostalService.MyTipax;

namespace Logiwall.Infrastructure.Providers.PostalServices;
public class MyTipaxProvider : PostalServiceBase
{
    private readonly string tokenUrl = "token";
    private readonly string loginUrl = "/api/v1/accounts/login";
    private readonly string inquiryUrl = "/api/v1/orders/inquiry";
    private string token = string.Empty;

    public MyTipaxProvider(IAppSettingService appSettingService,
        IHttpProvider httpProvider) : base(appSettingService, httpProvider)
    {
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
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var request = new MyTipaxGetTokenRequestDto
        {
            Username = appSettings.MyTipaxUsernameToken,
            Password = appSettings.MyTipaxPasswordToken,
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
        var header = await GetHeaderAsync();
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var request = new MyTipaxLoginRequestDto
        {
            Account = new MyTipaxLoginAccountDto
            {
                CellPhoneNumber = appSettings.MyTipaxUsernameLogin,
                Password = appSettings.MyTipaxPasswordLogin
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
}
