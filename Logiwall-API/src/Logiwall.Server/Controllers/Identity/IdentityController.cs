using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Logiwall.Application.Abstractions.Identity;
using Logiwall.Client.Core.Controllers.Identity;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Server.Resources;
using Logiwall.Shared.Dtos.Identity;
using Logiwall.Shared.Utils;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Logiwall.Shared.Dtos.Global;

namespace Logiwall.Server.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class IdentityController : AppControllerBase, IIdentityController
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IAuthenticationService authenticationService = default!;

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

        await signInManager.SignInAsync(user, false);
        var token = GenerateJwtToken(user);
        response.Data.Token = token;
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

    private string GenerateJwtToken(User user)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var certificatePath = Path.Combine(AppContext.BaseDirectory, "IdentityCertificate.pfx");
        var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, AppSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

        var issuer = AppSettings.IdentitySettings.Issuer;
        var audience = AppSettings.IdentitySettings.Audience;

        var validationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(certificate),

            RequireExpirationTime = true,
            ValidateLifetime = true,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuer = true,
            ValidIssuer = issuer,

            AuthenticationType = IdentityConstants.BearerScheme
        };

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                IssuedAt = DateTime.UtcNow,
                SigningCredentials = new SigningCredentials(validationParameters.IssuerSigningKey, SecurityAlgorithms.RsaSha512),
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                ]),
                Expires = DateTime.UtcNow.AddDays(1),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
