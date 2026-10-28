using Logiwall.Shared.Dtos.Identity;

namespace Logiwall.Client.Core.Controllers.Identity;

[Route("api/[controller]/[action]/")]
public interface IIdentityController : IAppController
{
    // The Blazor client is the admin panel (ADR-0005): admins sign in with a password, not with OTP.
    [HttpPost]
    Task<TokenResponseDto> GetToken(SignInRequestDto body, CancellationToken cancellationToken = default) => default!;
}
