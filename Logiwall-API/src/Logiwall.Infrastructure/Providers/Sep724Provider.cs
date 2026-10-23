using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers;
using Logiwall.Shared.Dtos.Providers.Sep724;

namespace Logiwall.Infrastructure.Providers;
public class Sep724Provider : ISep724Provider
{
    private short tryToGetToken = 1;
    private const short maxTryToGetToken = 3;
    private string getToken = "token";
    private string grantType = "password";
    private readonly string? terminal = "14385420";
    private readonly IAppSettingService appSettingService;
    private readonly IHttpProvider httpProvider;

    public Sep724Provider(IAppSettingService appSettingService,
        IHttpProvider httpProvider)
    {
        this.appSettingService = appSettingService;
        this.httpProvider = httpProvider;
    }

    public async Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto)
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
            Terminal = terminal,
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
            },failHandler: GetPaymentTokanFailHandler);

        return result;
    }

    public Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto)
    {
        throw new NotImplementedException();
    }

    private async Task<string?> GetTokenAsync()
    {
        var appSettings = await appSettingService.GetAllSettingsAsync();
        var header = new List<(string, string)>
        {
            new ("Authorization", $"Basic {appSettings.SepConnectAuthorization}")
        };

        var body = new List<KeyValuePair<string, string>>
        {
            new ("grant_type", grantType),
            new ("username" , appSettings.SepConnectUsername),
            new ("password" , appSettings.SepConnectPassword),
            new ("scope" , appSettings.SepConnectScope),
            new ("client" , appSettings.SepConnectClient),
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

        return result?.AccessToken;
    }
    private async Task<SepResponseBase<GetPaymentTokenResponseDto>?> GetPaymentTokanFailHandler(
        GetPaymentTokenRequestDto<GetPaymentTokenExtensionDto>? request,
        SepResponseBase<GetPaymentTokenResponseDto>? response)
    {
        if (response.Status == 401 && tryToGetToken < maxTryToGetToken)
        {
            tryToGetToken ++;
            return await GetPaymentTokenAsync(new PaymentTokenRequestDto
            {
                Amount = request.Amount,
                PhoneNumber = request.MobileNo,
                CallbackUrl = request.CallbackUrl,
                Channel= request.Channel,
                OrderNumber = request.Extensions.OrderNumber,
            });
        }

        return response;
    }
}
