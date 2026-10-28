using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.Sep724;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Logiwall.Infrastructure.Providers;
public class Sep724Provider : ISep724Provider
{
    private const string TokenCacheKey = "sep724:token";
    private const int MaxAttempts = 2;
    private string getToken = "token";
    private string grantType = "password";
    private readonly IAppSettingService appSettingService;
    private readonly IHttpProvider httpProvider;
    private readonly Sep724Options options;
    private readonly ProviderTokenCache tokenCache;

    public Sep724Provider(IAppSettingService appSettingService,
        IHttpProvider httpProvider,
        IOptions<Sep724Options> sep724Options,
        ProviderTokenCache tokenCache)
    {
        this.appSettingService = appSettingService;
        this.httpProvider = httpProvider;
        this.options = sep724Options.Value;
        this.tokenCache = tokenCache;
    }

    public Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto)
        => GetPaymentTokenAsync(requestDto, attempt: 1);

    private async Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto, int attempt)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return new SepResponseBase<GetPaymentTokenResponseDto>
            {
                Data = null,
                Message = "Invalid Client"
            };
        }

        var appSettings = await appSettingService.GetAllSettingsAsync();
        var header = new List<(string, string)>
        {
            new ("x-trace-id", Guid.NewGuid().ToString()),
            new ("Authorization", $"Bearer {token}")
        };

        var amount = (long)requestDto.Amount;
        var logiwallShare = amount * int.Parse(appSettings.LogiwallShare) / 100;
        var request = new GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>
        {
            Amount = amount,
            MobileNo = requestDto.PhoneNumber,
            Terminal = options.Terminal,
            Channel = requestDto.Channel,
            CallbackUrl = requestDto.CallbackUrl,
            Extensions = new GetPaymentTokenExtensionDto
            {
                OrderNumber = requestDto.OrderNumber,
                Accounts = new List<GetPaymentTokenAccount>
                {
                    new(){ Amount = logiwallShare, Iban = appSettings.LogiwallIban },
                    new(){ Amount = amount - logiwallShare, Iban = appSettings.MyTipaxIban}
                }
            }
        };

        var result = await httpProvider.PostAsync<GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>,
            SepResponseBase<GetPaymentTokenResponseDto>, GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>>(
            new HttpProviderRequest<GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>, GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>>
            {
                BaseAddress = appSettings.SepPayBaseUrl,
                Uri = getToken,
                Request = request,
                Body = request,
                HeaderParameters = header
            }, failHandler: async (_, response) =>
            {
                // A 401 means the cached access token has expired on SEP's side: refresh it and retry once.
                if (response?.Status == 401 && attempt < MaxAttempts)
                {
                    tokenCache.Invalidate(TokenCacheKey);
                    return await GetPaymentTokenAsync(requestDto, attempt + 1);
                }
                return response;
            });

        return result;
    }

    public Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto)
    {
        throw new NotImplementedException();
    }

    private Task<string?> GetTokenAsync() => tokenCache.GetOrCreateAsync(TokenCacheKey, async () =>
    {
        var result = await RequestTokenAsync();
        var lifetime = result?.ExpireIn > 0 ? TimeSpan.FromSeconds(result.ExpireIn) : TimeSpan.FromMinutes(5);
        return (result?.AccessToken, lifetime);
    });

    private async Task<GetTokenResponseDto?> RequestTokenAsync()
    {
        if (string.IsNullOrEmpty(options.Username) || string.IsNullOrEmpty(options.Password) || string.IsNullOrEmpty(options.BasicAuthorization))
            throw new InvalidOperationException("Providers:Sep724 credentials are not configured.");

        var appSettings = await appSettingService.GetAllSettingsAsync();
        var header = new List<(string, string)>
        {
            new ("Authorization", $"Basic {options.BasicAuthorization}")
        };

        var body = new List<KeyValuePair<string, string>>
        {
            new ("grant_type", grantType),
            new ("username" , options.Username),
            new ("password" , options.Password),
            new ("scope" , appSettings.SepConnectScope),
            new ("client" , options.Client),
        };
        var result = await httpProvider.PostAsync<GetTokenRequestDto, GetTokenResponseDto, List<KeyValuePair<string, string>>>(
            new HttpProviderRequest<List<KeyValuePair<string, string>>, GetTokenRequestDto>
            {
                Request = new GetTokenRequestDto(),
                Uri = getToken,
                BaseAddress = appSettings.SepConnectUrl,
                HeaderParameters = header,
                Body = body
            });

        return result;
    }
}
