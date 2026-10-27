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
    // Both tokens belong to Logiwall's single MyTipax service account, so every request can share them.
    private const string ApiTokenKey = "mytipax:token";
    private const string LoginTokenKey = "mytipax:login";
    // The MyTipax API returns no expiry for either token.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private const int MaxAttempts = 2;
    private readonly MyTipaxOptions options;
    private readonly ProviderTokenCache tokenCache;

    public override ProviderType ProviderType => ProviderType.MyTipax;

    public MyTipaxProvider(IAppSettingService appSettingService,
        IHttpProvider httpProvider,
        IOptions<MyTipaxOptions> myTipaxOptions,
        ProviderTokenCache tokenCache) : base(appSettingService, httpProvider)
    {
        this.options = myTipaxOptions.Value;
        this.tokenCache = tokenCache;
    }

    public override Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto)
        => InquireAsync(requestDto, attempt: 1);

    private async Task<List<OrderInquiryOptionDto>> InquireAsync(OrderInquiryRequestDto requestDto, int attempt)
    {
        var appSettings = await AppSettingService.GetAllSettingsAsync();
        var apiToken = await tokenCache.GetOrCreateAsync(ApiTokenKey, async () => (await GetTokenAsync(), TokenLifetime));
        var loginToken = await tokenCache.GetOrCreateAsync(LoginTokenKey, async () => (await LoginAsync(apiToken), TokenLifetime));
        var request = new MyTipaxOrderInquiryRequestDto
        {
            Order = requestDto,
            Token = loginToken,
            Version = appSettings.MyTipaxInquiryVersion
        };

        var status = System.Net.HttpStatusCode.OK;
        var result = await HttpProvider.PostAsync<MyTipaxOrderInquiryRequestDto, MyTipaxOrderInquiryResponseDto, MyTipaxOrderInquiryRequestDto>
            (new HttpProviderRequest<MyTipaxOrderInquiryRequestDto, MyTipaxOrderInquiryRequestDto>
            {
                BaseAddress = appSettings.MyTipaxBaseUrl,
                Body = request,
                HeaderParameters = Header(apiToken),
                Uri = inquiryUrl,
                Request = request
            }, postCallHandler: response => { status = response.StatusCode; return Task.CompletedTask; });

        // A 401 means a cached token has expired on MyTipax's side: refresh both and retry once.
        if (status == System.Net.HttpStatusCode.Unauthorized && attempt < MaxAttempts)
        {
            tokenCache.Invalidate(ApiTokenKey);
            tokenCache.Invalidate(LoginTokenKey);
            return await InquireAsync(requestDto, attempt + 1);
        }

        return result?.Order?.Options;
    }

    private async Task<string?> GetTokenAsync()
    {
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
        return response?.Token;
    }

    private async Task<string?> LoginAsync(string? apiToken)
    {
        EnsureCredentialsConfigured(options.LoginUsername, options.LoginPassword);
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
                HeaderParameters = Header(apiToken),
            });

        return response?.Token;
    }

    private static List<(string, string)> Header(string? apiToken) => new()
    {
        new ("Authorization", apiToken ?? string.Empty)
    };

    private static void EnsureCredentialsConfigured(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Providers:MyTipax credentials are not configured.");
    }
}
