using Logiwall.Application.Abstractions.People;
using Logiwall.Domain.Entities.People;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.People;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.People;
public class CustomerService : ICustomerService
{
    private readonly AppDbContext appDbContext;
    private readonly IStringLocalizer<AppStrings> localizer;

    public CustomerService(AppDbContext appDbContext,
        IStringLocalizer<AppStrings> localizer)
    {
        this.appDbContext = appDbContext;
        this.localizer = localizer;
    }
    public async Task<ServiceResponse<bool>> CreateCustomerContactAsync(CreateCustomerContactRequestDto requestDto)
    {
        var person = await appDbContext.People.Select(t => new { t.UserId, CustomerId = t.Customer.Id }).FirstOrDefaultAsync(t => t.UserId == requestDto.UserId);
        if (person is null)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
            };
        }

        var contactInfo = new CustomerContact
        {
            CustomerId = person.CustomerId,
            CityId = requestDto.CityId,
            CountryId = requestDto.CountryId,
            Address = requestDto.Address,
            PhoneNumber = requestDto.PhoneNumber,
            Plaque = requestDto.Plaque,
            PostalCode = requestDto.PostalCode,
            Lan = requestDto.Lan,
            Lat = requestDto.Lat,
            Title = requestDto.Title,
            Unit = requestDto.Unit,
            IsArchive = false,
        };
        //todo add a method to set an address to default 
        await appDbContext.AddAsync(contactInfo);
        await appDbContext.SaveChangesAsync();

        return new ServiceResponse<bool>
        {
            Data = true,
            Succuss = true
        };
    }

    public async Task<ServiceResponse<CustomerContactResponseDto>> GetCustomerContactByIdAsync(int id, int userId)
    {
        var contactInfo = await appDbContext.CustomerContacts.FirstOrDefaultAsync(t => t.Id == id && t.Customer.Person.UserId == userId);
        if (contactInfo is null)
        {
            return new ServiceResponse<CustomerContactResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.ContactInfoNotFound)]
            };
        }
        var result = new CustomerContactResponseDto
        {
            Address = contactInfo.Address,
            CityId = contactInfo.CityId,
            CountryId = contactInfo.CountryId,
            CustomerId = contactInfo.CustomerId,
            PostalCode = contactInfo.PostalCode,
            IsDefualt = contactInfo.IsDefualt,
            Lan = contactInfo.Lan,
            Lat = contactInfo.Lat,
            Id = contactInfo.Id,
            PhoneNumber = contactInfo.PhoneNumber,
            Plaque = contactInfo.Plaque,
            Title = contactInfo.Title,
            IsArchive = contactInfo.IsArchive,
            Unit = contactInfo.Unit,
        };
        return new ServiceResponse<CustomerContactResponseDto>
        {
            Data = result,
            Succuss = true
        };
    }

    public async Task<ServiceResponse<IEnumerable<CustomerContactResponseDto>>> GetCustomerContactListAsync(GetCustomerContactListRequestDto requestDto)
    {
        var person = await appDbContext.People.Select(t =>new {t.UserId, CustomerId =  t.Customer.Id }).FirstOrDefaultAsync(t => t.UserId == requestDto.UserId);
        if (person is null)
        {
            return new ServiceResponse<IEnumerable<CustomerContactResponseDto>>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
            };
        }
        var contactList = await appDbContext.CustomerContacts.Where(t => t.CustomerId == person.CustomerId)
            .Select(t => new CustomerContactResponseDto {
                Id = t.Id,
                Address = t.Address,
                CityId = t.CityId,
                CountryId = t.CountryId,
                CustomerId = t.CustomerId,
                IsDefualt = t.IsDefualt,
                Lan = t.Lan,
                Lat = t.Lat,
                PhoneNumber = t.PhoneNumber,
                Plaque = t.Plaque,
                PostalCode = t.PostalCode,
                Title = t.Title,
                Unit = t.Unit,
            }).ToListAsync();
        return new ServiceResponse<IEnumerable<CustomerContactResponseDto>>
        {
            Data = contactList,
            Succuss = true
        };
    }

    public async Task<ServiceResponse<bool>> UpdateCustomerContactAsync(UpdateCustomerContactRequestDto requestDto, int userId)
    {
        var contactInfo = await appDbContext.CustomerContacts.FirstOrDefaultAsync(t => t.Id == requestDto.Id && t.Customer.Person.UserId == userId);
        if (contactInfo is null)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Succuss = false,
                Message = localizer[nameof(AppStrings.ContactInfoNotFound)]
            };
        }
        contactInfo.Title = requestDto.Title;
        contactInfo.Address = requestDto.Address;
        contactInfo.CityId = requestDto.CityId;
        contactInfo.CountryId = requestDto.CountryId;
        contactInfo.Lat = requestDto.Lat;
        contactInfo.Lan = requestDto.Lan;
        contactInfo.PostalCode = requestDto.PostalCode;
        //contactInfo.IsDefualt = requestDto.IsDefualt;
        contactInfo.ModificationDate = DateTimeOffset.Now;
        contactInfo.PhoneNumber = requestDto.PhoneNumber;
        contactInfo.Plaque = requestDto.Plaque;
        contactInfo.Unit = requestDto.Unit;

        appDbContext.Update(contactInfo);
        await appDbContext.SaveChangesAsync();

        return new ServiceResponse<bool>
        {
            Data = true,
            Succuss = true
        };
    }
}
