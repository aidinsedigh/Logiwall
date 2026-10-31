using Logiwall.Application.Abstractions.Identity;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Server.Resources;
using Logiwall.Shared.Dtos.Identity;
using Logiwall.Shared.Utils;
using Microsoft.AspNetCore.Authentication.BearerToken;
using AuthenticationProperties = Microsoft.AspNetCore.Authentication.AuthenticationProperties;
using AuthenticationTicket = Microsoft.AspNetCore.Authentication.AuthenticationTicket;
using Microsoft.AspNetCore.RateLimiting;
using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Server.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class IdentityController : AppControllerBase
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IAuthenticationService authenticationService = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [HttpPost, EnableRateLimiting("otp-send")]
    public async Task<ActionResult> SendOtpCode(SendOtpRequestDto requestDto, CancellationToken cancellationToken)
    {
        var response = await authenticationService.SendOtpCodeAsync(requestDto);
        if(!response.Succuss)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost, EnableRateLimiting("auth-verify")]
    public async Task<ActionResult> ConfirmationOtpCode(ConfirmaionOtpRequestDto requestDto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(requestDto.PhoneNumber);
        if (user is null)
        {
            // Same answer as a wrong code, so the response does not reveal which numbers exist.
            return BadRequest(new ServiceResponse<ConfirmationOtpResponseDto> { Succuss = false, Message = Localizer[nameof(AppStrings.OtpCodeIsNotCorrect)] });
        }

        if (user.IsArchive || user.Active is false)
            return BadRequest(new ServiceResponse<ConfirmationOtpResponseDto> { Succuss = false, Message = Localizer[nameof(AppStrings.AccountIsDisabled)] });

        requestDto.Code = Globals.ConvertPersianNumbersToEnglish(requestDto.Code);
        var response = await authenticationService.ConfirmationOtpCodeAsync(requestDto);

        if(!response.Succuss)
        {
            return BadRequest(response);
        }

        // The same protector as GetToken signs the token, so both login paths issue one kind of token.
        var options = bearerTokenOptions.Get(IdentityConstants.BearerScheme);
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow + options.BearerTokenExpiration };
        response.Data.AccessToken = options.BearerTokenProtector.Protect(new AuthenticationTicket(principal, properties, IdentityConstants.BearerScheme));
        response.Data.ExpiresIn = (long)options.BearerTokenExpiration.TotalSeconds;
        return Ok(response);
    }

    [HttpPost, ProducesResponseType<TokenResponseDto>(statusCode: 200), EnableRateLimiting("auth-verify")]
    public async Task GetToken(SignInRequestDto signInRequest)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        // A disabled account gets the bad-password answer, so the response does not reveal that it exists.
        var existingUser = await userManager.FindByNameAsync(signInRequest.UserName!);
        if (existingUser is not null && (existingUser.IsArchive || existingUser.Active is false))
            throw new UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)));

        var result = await signInManager.PasswordSignInAsync(signInRequest.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var user = await userManager.FindByNameAsync(signInRequest.UserName!);
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));
        }

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)));

        await authenticationService.LogUserLoginAsync(new LogUserLoginRequestDto
        {
            Channel = signInRequest.Channel,
            PhoneNumber = signInRequest.UserName,
            LoginProvider = signInRequest.LoginProvider,
        });
    }
}
