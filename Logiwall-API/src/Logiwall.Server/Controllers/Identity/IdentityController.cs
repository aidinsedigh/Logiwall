using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentEmail.Core;
using Logiwall.Application.Abstractions.Identity;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Server.Resources;
using Logiwall.Shared.Dtos.Identity;
using Logiwall.Shared.Utils;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

namespace Logiwall.Server.Controllers.Identity;

[Microsoft.AspNetCore.Mvc.Route("api/[controller]/[action]")]
[ApiController, AllowAnonymous]
public partial class IdentityController : AppControllerBase
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private SignInManager<User> signInManager = default!;

    [AutoInject] private IFluentEmail fluentEmail = default!;

    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [AutoInject] private IStringLocalizer<IdentityStrings> identityLocalizer = default!;

    [AutoInject] private IOptionsMonitor<BearerTokenOptions> bearerTokenOptions = default!;

    [AutoInject] private IAuthenticationService authenticationService = default!;

    [AutoInject] private IDataProtectionProvider dataProtectionProvider = default!;

    [HttpPost]
    public async Task<ActionResult> SendOtpCode(SendOtpRequestDto requestDto, CancellationToken cancellationToken)
    {
        var response = await authenticationService.SendOtpCodeAsync(requestDto);
        if(!response.Succuss)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult> ConfirmationOtpCode(ConfirmaionOtpRequestDto requestDto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(requestDto.PhoneNumber);
        if (user is null)
        {
            return BadRequest(string.Format(AppStrings.UserNameNotFound, requestDto.PhoneNumber));
        }

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

    /// <summary>
    /// By leveraging summary tags in your controller's actions and DTO properties you can make your codes much easier to maintain.
    /// These comments will also be used in swagger docs and ui.
    /// </summary>
    [HttpPost]
    public async Task SignUp(SignUpRequestDto signUpRequest, CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByNameAsync(signUpRequest.Email!);

        var userToAdd = signUpRequest.Map();

        if (existingUser is not null)
        {
            if (await userManager.IsEmailConfirmedAsync(existingUser))
            {
                throw new BadRequestException(Localizer.GetString(nameof(AppStrings.DuplicateEmail), existingUser.Email!));
            }
            else
            {
                var deleteResult = await userManager.DeleteAsync(existingUser);
                if (!deleteResult.Succeeded)
                    throw new ResourceValidationException(deleteResult.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
                //userToAdd.ConfirmationEmailRequestedOn = existingUser.ConfirmationEmailRequestedOn;
            }
        }

        userToAdd.LockoutEnabled = true;

        var result = await userManager.CreateAsync(userToAdd, signUpRequest.Password!);

        if (result.Succeeded is false)
        {
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
        }

        //await SendConfirmationEmail(new() { Email = userToAdd.Email }, userToAdd, cancellationToken);
    }

    //[HttpPost]
    //public async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, CancellationToken cancellationToken)
    //{
    //    var user = await userManager.FindByEmailAsync(sendConfirmationEmailRequest.Email!);

    //    if (user is null)
    //        throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendConfirmationEmailRequest.Email!));

    //    if (await userManager.IsEmailConfirmedAsync(user))
    //        throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)]);

    //    await SendConfirmationEmail(sendConfirmationEmailRequest, user, cancellationToken);
    //}

    //private async Task SendConfirmationEmail(SendConfirmationEmailRequestDto sendConfirmationEmailRequest, User user, CancellationToken cancellationToken)
    //{
    //    //var resendDelay = (DateTimeOffset.Now - user.ConfirmationEmailRequestedOn) - AppSettings.IdentitySettings.ConfirmationEmailResendDelay;

    //    //if (resendDelay < TimeSpan.Zero)
    //    //    throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForConfirmationEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

    //    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

    //    var controller = RouteData.Values["controller"]!.ToString();

    //    var confirmationLink = new Uri(HttpContext.Request.GetBaseUrl(), $"email-confirmation?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(token)}");

    //    var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
    //    {
    //        //var renderedComponent = await htmlRenderer.RenderComponentAsync<EmailConfirmationTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
    //        //{
    //        //    {   nameof(EmailConfirmationTemplate.Model),
    //        //        new EmailConfirmationModel
    //        //        {
    //        //            ConfirmationLink = confirmationLink
    //        //        }
    //        //    },
    //        //    { nameof(HttpContext), HttpContext }
    //        //}));

    //        //return renderedComponent.ToHtmlString();
    //        return string.Empty;
    //    });

    //    var result = await fluentEmail
    //        .Subject(emailLocalizer[EmailStrings.ConfirmationEmailSubject])
    //        .Body(body, isHtml: true)
    //        .SendAsync(cancellationToken);

    //    //user.ConfirmationEmailRequestedOn = DateTimeOffset.Now;

    //    await userManager.UpdateAsync(user);

    //    if (!result.Successful)
    //        throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    //}

    //[HttpPost]
    //public async Task ConfirmEmail(ConfirmEmailRequestDto body)
    //{
    //    var user = await userManager.FindByEmailAsync(body.Email!);

    //    if (user is null)
    //        throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), body.Email!));

    //    var emailConfirmed = user.EmailConfirmed;

    //    if (emailConfirmed is false)
    //    {
    //        var result = await userManager.ConfirmEmailAsync(user, body.Token!);
    //        if (!result.Succeeded)
    //            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    //    }
    //}

    [HttpPost, ProducesResponseType<TokenResponseDto>(statusCode: 200)]
    public async Task GetToken(SignInRequestDto signInRequest)
    {
        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(signInRequest.UserName!, signInRequest.Password!, isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var user = await userManager.FindByNameAsync(signInRequest.UserName!);
            throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user!.LockoutEnd!).Value.ToString("mm\\:ss")));
        }

        /* if (result.RequiresTwoFactor)
        {
            if (!string.IsNullOrEmpty(signInRequest.TwoFactorCode))
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(signInRequest.TwoFactorCode, rememberClient: true);
            }
            else if (!string.IsNullOrEmpty(signInRequest.TwoFactorRecoveryCode))
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(signInRequest.TwoFactorRecoveryCode);
            }
        } */

        if (result.Succeeded is false)
            throw new UnauthorizedException(Localizer.GetString(nameof(AppStrings.InvalidUsernameOrPassword)));

        await authenticationService.LogUserLoginAsync(new LogUserLoginRequestDto
        {
            Channel = signInRequest.Channel,
            PhoneNumber = signInRequest.UserName,
            LoginProvider = signInRequest.LoginProvider,
        });
    }

    //[HttpPost]
    //public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto refreshRequest)
    //{
    //    var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
    //    var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

    //    if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc ||
    //            await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not User user)
    //    {
    //        return base.Challenge();
    //    }

    //    var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);

    //    return SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
    //}

    //[HttpPost]
    //public async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto sendResetPasswordEmailRequest
    //      , CancellationToken cancellationToken)
    //{
    //    var user = await userManager.FindByEmailAsync(sendResetPasswordEmailRequest.Email!);

    //    if (user is null)
    //        throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), sendResetPasswordEmailRequest.Email!));

    //    //var resendDelay = (DateTimeOffset.Now - user.ResetPasswordEmailRequestedOn) - AppSettings.IdentitySettings.ResetPasswordEmailResendDelay;

    //    //if (resendDelay < TimeSpan.Zero)
    //    //    throw new TooManyRequestsExceptions(Localizer.GetString(nameof(AppStrings.WaitForResetPasswordEmailResendDelay), resendDelay.Value.ToString("mm\\:ss")));

    //    var token = await userManager.GeneratePasswordResetTokenAsync(user);

    //    var resetPasswordLink = new Uri(HttpContext.Request.GetBaseUrl(), $"reset-password?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(token)}");

    //    var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
    //    {
    //        //var renderedComponent = await htmlRenderer.RenderComponentAsync<ResetPasswordTemplate>(ParameterView.FromDictionary(new Dictionary<string, object?>()
    //        //{
    //        //    { nameof(ResetPasswordTemplate.Model),
    //        //        new ResetPasswordModel
    //        //        {
    //        //            DisplayName = user.DisplayName,
    //        //            ResetPasswordLink = resetPasswordLink
    //        //        }
    //        //    },
    //        //    { nameof(HttpContext) , HttpContext }
    //        //}));

    //        //return renderedComponent.ToHtmlString();
    //        return string.Empty;
    //    });

    //    var result = await fluentEmail
    //        .Subject(emailLocalizer[EmailStrings.ResetPasswordEmailSubject])
    //        .Body(body, isHtml: true)
    //        .SendAsync(cancellationToken);


    //    await userManager.UpdateAsync(user);

    //    if (!result.Successful)
    //        throw new ResourceValidationException(result.ErrorMessages.Select(err => Localizer[err]).ToArray());
    //}

    //[HttpPost]
    //public async Task ResetPassword(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken)
    //{
    //    var user = await userManager.FindByEmailAsync(resetPasswordRequest.Email!);

    //    if (user is null)
    //        throw new BadRequestException(Localizer.GetString(nameof(AppStrings.UserNameNotFound), resetPasswordRequest.Email!));

    //    var result = await userManager.ResetPasswordAsync(user, resetPasswordRequest.Token!, resetPasswordRequest.Password!);

    //    if (!result.Succeeded)
    //        throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    //}

    private string GenerateJwtToken(User user)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        var certificate = new X509Certificate2(certificatePath, AppSettings.IdentitySettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet);

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
