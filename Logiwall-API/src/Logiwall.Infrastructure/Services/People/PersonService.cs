using Logiwall.Application.Abstractions.People;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.People;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logiwall.Infrastructure.Services.People;
public class PersonService(AppDbContext dbContext,
    UserManager<User> userManager,
    ILogger<PersonService> logger,
    IStringLocalizer<AppStrings> localizer) : IPersonService
{
    public async Task<ServiceResponse<bool>> CreatePersonAsync(CreatePersonRequestDto requestDto)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(t=> t.UserId == requestDto.UserId);
        if (person is not null)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = localizer[nameof(AppStrings.YouRegisteredBefore)]
            };
        }

        // The unique index stays the last guard; this check gives a clear answer instead of HTTP 500.
        if (await dbContext.PersonalInfos.AnyAsync(p => p.NationalCode == requestDto.NationalCode))
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = localizer[nameof(AppStrings.NationalCodeIsTaken)]
            };
        }

        person = new Person
        {
            Name = requestDto.Name,
            LastName = requestDto.LastName,
            UserId = requestDto.UserId,
            PhoneNumber = requestDto.PhoneNumber,
            Customer = new Customer
            {
                Email = requestDto.Email
            },
            PersonalInfo = new PersonalInfo
            {
                NationalCode = requestDto.NationalCode,
                Kind = Constants.PersonKind.Private,
            }
        };
        await dbContext.AddAsync(person);
        await dbContext.SaveChangesAsync();

        return new ServiceResponse<bool>
        {
            Data = true,
            Succuss = true
        };
    }

    public async Task<ServiceResponse<UserProfileDto>> GetUserProfileAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new ServiceResponse<UserProfileDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
            };
        }
        var person = await dbContext.People.Include(t => t.PersonalInfo).FirstOrDefaultAsync(t => t.UserId == userId);

        if (person is null)
        {
            return new ServiceResponse<UserProfileDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
            };
        }

        var userProfile = new UserProfileDto
        {
            UserId = userId,
            Username = user.UserName,
            FirstName = person.Name,
            LastName = person.LastName,
            PhoneNumber = person.PhoneNumber,
            BirthDate = person.PersonalInfo?.BirthDate,
            Gender = person.PersonalInfo?.Gender,
            Kind = person.PersonalInfo?.Kind ?? Constants.PersonKind.Private,
            NationalCode = person.PersonalInfo?.NationalCode,
            Email = user.Email,
        };
        return new ServiceResponse<UserProfileDto>
        {
            Data = userProfile,
            Succuss = true,
            Message = string.Empty,
        };
    }

    public async Task<ServiceResponse<UserProfileDto>> UpdateUserProfileAsync(UserProfileDto requestDto)
    {
        if (requestDto is null)
        {
            throw new ArgumentNullException();
        }
        var user = await userManager.FindByIdAsync(requestDto.UserId!.ToString());
        if (user is null)
        {
            return new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UserNotFound)]
            };
        }
        try
        {
            // The email change and the person rows are saved together; disposing without a commit rolls back.
            await using var tx = await dbContext.Database.BeginTransactionAsync();

            var setEmail = await userManager.SetEmailAsync(user, requestDto.Email);
            if (setEmail.Succeeded is false)
            {
                return new ServiceResponse<UserProfileDto> { Data = requestDto, Succuss = false, Message = localizer[nameof(AppStrings.EmailAddressAttribute_ValidationError), localizer[nameof(AppStrings.Email)]] };
            }

            var person = await dbContext.People.Include(t => t.PersonalInfo).FirstOrDefaultAsync(t => t.UserId == requestDto.UserId);
            if (person is null)
            {
                return new ServiceResponse<UserProfileDto>
                {
                    Data = null,
                    Succuss = false,
                    Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
                };
            }

            person.Name = requestDto.FirstName;
            person.LastName = requestDto.LastName;
            person.PhoneNumber = requestDto.PhoneNumber;
            dbContext.Update(person);

            var personalInfo = person.PersonalInfo;
            if (personalInfo is null)
            {
                personalInfo = new PersonalInfo
                {
                    BirthDate = requestDto.BirthDate,
                    Kind = requestDto.Kind,
                    Gender = requestDto.Gender,
                    NationalCode = requestDto.NationalCode,
                    PersonId = person.Id,
                };
                await dbContext.AddAsync(personalInfo);
            }
            else
            {
                personalInfo.BirthDate = requestDto.BirthDate;
                personalInfo.Gender = requestDto.Gender;
                personalInfo.Kind = requestDto.Kind;
                personalInfo.NationalCode = requestDto.NationalCode;

                dbContext.Update(personalInfo);
            }
            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = true,
                Message = localizer[nameof(AppStrings.ProfileIsUpdated)]
            };
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "[Person][UpdateUserProfile][{UserId}] Profile update failed.", requestDto.UserId);
            return new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UnknownException)]
            };
        }
        
    }
}
