using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Logiwall.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication;

namespace Logiwall.Server.Services;

public class ShippingWebhookAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IOptions<ETipaxOptions> carrier)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ShippingWebhook";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = carrier.Value.WebhookSecret;
        var actual = Request.Headers["X-Tipax-Webhook-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expected) || actual.Length > 512 ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(actual))))
            return Task.FromResult(AuthenticateResult.Fail("Invalid shipping webhook credential."));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "etipax")], SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
