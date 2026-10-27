using Logiwall.Application.Abstractions.People;
using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.People;
using Logiwall.Server.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logiwall.Infrastructure.Services.People;
public class PersonService : IPersonService
{
    private readonly AppDbContext dbContext;
    private readonly UserManager<User> userManager;
    private readonly ILogger<PersonService> logger;

    public PersonService(AppDbContext dbContext,
        UserManager<User> userManager,
        ILogger<PersonService> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.logger = logger;
    }
    public async Task<ServiceResponse<bool>> CreatePersonAsync(CreatePersonRequestDto requestDto)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(t=> t.UserId == requestDto.UserId);
        if (person is not null)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = AppStrings.YouRegisteredBefore
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
                Message = "User is not registered"
            };
        }
        var person = await dbContext.People.Include(t => t.PersonalInfo).FirstOrDefaultAsync(t => t.UserId == userId);

        if (person is null)
        {
            return new ServiceResponse<UserProfileDto>
            {
                Data = null,
                Succuss = false,
                Message = "User is not registered"
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
                Message = "User Not Found"
            };
        }
        try
        {
            user.Email = requestDto.Email;
            user.NormalizedEmail = requestDto.Email?.Normalize();
            await userManager.UpdateAsync(user);

            var person = await dbContext.People.Include(t => t.PersonalInfo).FirstOrDefaultAsync(t => t.UserId == requestDto.UserId);
            if (person is null)
            {
                return new ServiceResponse<UserProfileDto>
                {
                    Data = null,
                    Succuss = false,
                    Message = "User is not registered"
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

            return new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = true,
                Message = "Profile is updated"
            };
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "UpdateUserProfileAsync failed");
            return new ServiceResponse<UserProfileDto>
            {
                Data = requestDto,
                Succuss = false,
                Message = "An unexpected error occurred."
            };
        }
        
    }
}
