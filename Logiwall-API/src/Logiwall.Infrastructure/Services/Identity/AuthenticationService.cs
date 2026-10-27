using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Identity;
using Logiwall.Application.Abstractions.Tools;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.People;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logiwall.Infrastructure.Services.Tools;

namespace Logiwall.Infrastructure.Services.Identity;
public class AuthenticationService : IAuthenticationService
{
    private const int MaxOtpAttempts = 5;
    private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan OtpResendCooldown = TimeSpan.FromSeconds(60);

    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;
    private readonly ISMSService smsService;
    private readonly IAppSettingService appSettingService;
    private readonly IStringLocalizer<AppStrings> localizer;
    private readonly ILogger<AuthenticationService> logger;

    public AuthenticationService(AppDbContext dbContext,
        UserManager<User> userManager,
        ISMSService smsService,
        IAppSettingService appSettingService,
        IStringLocalizer<AppStrings> localizer,
        ILogger<AuthenticationService> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.smsService = smsService;
        this.appSettingService = appSettingService;
        this.localizer = localizer;
        this.logger = logger;
    }
    public async Task<ServiceResponse<bool>> SendOtpCodeAsync([NotNull] SendOtpRequestDto requestDto)
    {
        try
        {
            var user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);

            // The cooldown check must run before the SMS is sent: each SMS costs money.
            if (user is not null)
            {
                var lastTimestamp = await userManager.GetAuthenticationTokenAsync(user, "OTP", "timestamp");
                if (DateTimeOffset.TryParse(lastTimestamp, CultureInfo.InvariantCulture, out var lastExpiry)
                    && lastExpiry - OtpValidity + OtpResendCooldown > DateTimeOffset.Now)
                {
                    return new ServiceResponse<bool> { Data = false, Succuss = false, Message = localizer.GetString(nameof(AppStrings.OtpResendCooldown)) };
                }
            }

            if (user is not null && (user.IsArchive || user.Active is false))
            {
                return new ServiceResponse<bool> { Data = false, Succuss = false, Message = localizer.GetString(nameof(AppStrings.AccountIsDisabled)) };
            }

            if (user is null)
            {
                // OTP users have no password; password sign-in (GetToken) is only possible after Person/Register sets one.
                var createResult = await userManager.CreateAsync(new User
                {
                    UserName = requestDto.PhoneNumber,
                    NormalizedUserName = requestDto.PhoneNumber,
                    PhoneNumber = requestDto.PhoneNumber,
                    Active = true
                });

                if (createResult.Succeeded is false)
                {
                    logger.LogWarning("[Authentication][SendOtpCode][{ErrorCodes}] OTP user was not created.", string.Join(", ", createResult.Errors.Select(e => e.Code)));
                    return new ServiceResponse<bool>
                    {
                        Data = false,
                        Succuss = false,
                        Message = localizer.GetString(nameof(AppStrings.OtpServiceIsNotConnected))
                    };
                }

                user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);
            }

            // Store the code before the SMS goes out, so a delivered code always works.
            var code = GenerateRandomCode();
            var expiryTime = DateTimeOffset.Now.Add(OtpValidity);
            var userOtp = new UserOtp
            {
                Code = code,
                ExpirationDate = expiryTime,
                UserId = user.Id,
                Channel = requestDto.Channel,
                LoginProvider = requestDto.LoginProvider,
            };
            await dbContext.AddAsync(userOtp);
            await dbContext.SaveChangesAsync();

            await userManager.SetAuthenticationTokenAsync(user, "OTP", "otp", code);
            await userManager.SetAuthenticationTokenAsync(user, "OTP", "timestamp", expiryTime.ToString("o"));
            await userManager.SetAuthenticationTokenAsync(user, "OTP", "attempts", "0");

            var settings = await appSettingService.GetAllSettingsAsync();
            var message = settings.LoginOtpText?.Replace("{CODE}", code);
            var smsResult = await smsService.SendSmsAsync(message ?? code, requestDto.PhoneNumber);
            if (smsResult != "OK")
            {
                logger.LogWarning("[Authentication][SendOtpCode][{Destination}] OTP SMS was not sent.", SMSService.MaskPhone(requestDto.PhoneNumber));
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Succuss = false,
                    Message = localizer.GetString(nameof(AppStrings.OtpServiceIsNotConnected))
                };
            }

            return new ServiceResponse<bool>
            {
                Data = true,
                Succuss = true,
                Message = localizer.GetString(nameof(AppStrings.OtpCodeIsSent))
            };
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "[Authentication][SendOtpCode] Sending the OTP failed.");
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = localizer.GetString(nameof(AppStrings.OtpServiceIsNotConnected))
            };
        }
    }

    public async Task<ServiceResponse<ConfirmationOtpResponseDto>> ConfirmationOtpCodeAsync(ConfirmaionOtpRequestDto requestDto)
    {
        var user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);
        if (user is null)
        {
            // Same answer as a wrong code, so the response does not reveal which numbers exist.
            return new ServiceResponse<ConfirmationOtpResponseDto>
            {
                Succuss = false,
                Message = localizer.GetString(nameof(AppStrings.OtpCodeIsNotCorrect))
            };
        }
        //var otp = await dbContext.UserOtps.Where(t => t.UserId == user.Id);
        //if (requestDto.Code != otp.Code)
        //{
        //    return new ServiceResponse<ConfirmationOtpResponseDto>
        //    {
        //        Succuss = false,
        //        Message = AppStrings.OtpCodeIsNotCorrect
        //    };
        //}
        var storedOTP = await userManager.GetAuthenticationTokenAsync(user, "OTP", "otp");
        var attemptsText = await userManager.GetAuthenticationTokenAsync(user, "OTP", "attempts");
        _ = int.TryParse(attemptsText, out var attempts);

        if (storedOTP is null || attempts >= MaxOtpAttempts || CodesEqual(storedOTP, requestDto.Code) is false)
        {
            attempts++;
            if (storedOTP is not null && attempts >= MaxOtpAttempts)
            {
                // Too many wrong guesses: burn the code so the remaining keyspace can't be searched.
                await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "otp");
                await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "timestamp");
            }
            await userManager.SetAuthenticationTokenAsync(user, "OTP", "attempts", attempts.ToString(CultureInfo.InvariantCulture));

            return new ServiceResponse<ConfirmationOtpResponseDto>
            {
                Succuss = false,
                Message = localizer.GetString(nameof(AppStrings.OtpCodeIsNotCorrect))
            };
        }
        var storedTimestamp = await userManager.GetAuthenticationTokenAsync(user, "OTP", "timestamp");

        var isStoredTimeStamp = DateTimeOffset.TryParse(storedTimestamp, out DateTimeOffset timeOffset);
        if (!isStoredTimeStamp || DateTimeOffset.Now > timeOffset)
        {
            return new ServiceResponse<ConfirmationOtpResponseDto>
            {
                Succuss = false,
                Message = localizer.GetString(nameof(AppStrings.OtpCodeIsExpired)),
            };
        }

        await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "otp");
        await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "timestamp");
        await userManager.RemoveAuthenticationTokenAsync(user, "OTP", "attempts");

        var person = await dbContext.People.FirstOrDefaultAsync(t => t.UserId == user.Id);
        await LogUserLoginAsync(new LogUserLoginRequestDto
        {
            Channel = requestDto.Channel,
            PhoneNumber = requestDto.PhoneNumber,
            LoginProvider = requestDto.LoginProvider,
        });

        if (person != null)
        {
            return new ServiceResponse<ConfirmationOtpResponseDto>
            {
                Succuss = true,
                Message = localizer.GetString(nameof(AppStrings.OtpCodeIsSent)),
                Data = new ConfirmationOtpResponseDto
                {
                    FullName = person.FullName,
                }
            };
        }
        //todo: login show dashboard

        return new ServiceResponse<ConfirmationOtpResponseDto>
        {
            Data = new ConfirmationOtpResponseDto { },
            Succuss = true,
            Message = localizer.GetString(nameof(AppStrings.OtpCodeIsSent))
        }; //todo

    }

    public async Task LogUserLoginAsync(LogUserLoginRequestDto requestDto)
    {
        User? user = null;
        try
        {
            user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);

            var userlogin = new UserLogin
            {
                Channel = requestDto.Channel,
                ProviderKey = DateTimeOffset.Now.ToString(),
                ProviderDisplayName = string.Empty,
                UserId = user.Id,
                LoginProvider = Enum.GetName(requestDto.LoginProvider) 
            };

            await dbContext.AddAsync(userlogin);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "[Authentication][LogUserLogin][{UserId}] Login record was not saved.", user?.Id);
        }
    }

    private static bool CodesEqual(string expected, string? actual)
        => actual is not null && CryptographicOperations.FixedTimeEquals(
               Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));

    private static string GenerateRandomCode()
    {
        return RandomNumberGenerator.GetInt32(10000, 99999).ToString();
    }
}
