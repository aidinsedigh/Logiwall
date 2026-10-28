using System.Text;
using System.Text.Json;
using Logiwall.Shared.Dtos.Identity;
using Logiwall.Client.Core.Controllers.Identity;

namespace Logiwall.Client.Core.Services;

public partial class AuthenticationManager : AuthenticationStateProvider
{
    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;

    public async Task SignIn(SignInRequestDto signInModel, CancellationToken cancellationToken)
    {
        var result = await identityController.GetToken(signInModel, cancellationToken);

        // GetToken also signs in customers (the React app uses it). The server checks the role on
        // every admin endpoint; this check only keeps a customer out of the panel UI.
        if (ParseTokenClaims(result.AccessToken!).Any(claim => claim is { Type: "role", Value: "Admin" }) is false)
            throw new ForbiddenException(localizer[nameof(AppStrings.ForbiddenException)]);

        await StoreToken(result.AccessToken!, signInModel.RememberMe);

        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task SignOut()
    {
        await storageService.RemoveItem("access_token");
        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Remove("access_token");
        }
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task RefreshToken()
    {
        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Remove("access_token");
        }
        await storageService.RemoveItem("access_token");
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // The server has no refresh endpoint: an expired token signs the user out.
        var access_token = await tokenProvider.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(access_token))
        {
            return NotSignedIn();
        }

        var identity = new ClaimsIdentity(claims: ParseTokenClaims(access_token), authenticationType: "Bearer", nameType: "name", roleType: "role");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private async Task StoreToken(string accessToken, bool rememberMe)
    {
        await storageService.SetItem("access_token", accessToken, rememberMe);
        if (AppRenderMode.PrerenderEnabled && AppRenderMode.IsBlazorHybrid is false)
        {
            await cookie.Set(new()
            {
                Name = "access_token",
                Value = accessToken,
                SameSite = SameSite.Strict,
                Secure = BuildConfiguration.IsRelease()
            });
        }
    }

    private static AuthenticationState NotSignedIn()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private IEnumerable<Claim> ParseTokenClaims(string access_token)
    {
        // A user with more than one role gets a JSON array, for example "role": ["Admin", "Support"].
        return ParseJwt(access_token)
            .SelectMany(keyValue => keyValue.Value is JsonElement { ValueKind: JsonValueKind.Array } values
                ? values.EnumerateArray().Select(value => new Claim(keyValue.Key, value.ToString()))
                : [new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty)])
            .ToArray();
    }

    private Dictionary<string, object> ParseJwt(string access_token)
    {
        // Split the token to get the payload
        string base64UrlPayload = access_token.Split('.')[1];

        // Convert the payload from Base64Url format to Base64
        string base64Payload = ConvertBase64UrlToBase64(base64UrlPayload);

        // Decode the Base64 string to get a JSON string
        string jsonPayload = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));

        // Deserialize the JSON string to a dictionary
        var claims = JsonSerializer.Deserialize(jsonPayload, jsonSerializerOptions.GetTypeInfo<Dictionary<string, object>>())!;

        return claims;
    }

    private static string ConvertBase64UrlToBase64(string base64Url)
    {
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');

        // Adjust base64Url string length for padding
        switch (base64Url.Length % 4)
        {
            case 2:
                base64Url += "==";
                break;
            case 3:
                base64Url += "=";
                break;
        }

        return base64Url;
    }
}
