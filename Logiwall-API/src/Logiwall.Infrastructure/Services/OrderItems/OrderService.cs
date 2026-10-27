using System.Reflection;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Server.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
using Logiwall.Shared.Dtos.Providers.PostalService;
using Logiwall.Shared.Dtos.Providers.Sep724;
using Logiwall.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Infrastructure.Services.OrderItems;
public class OrderService : IOrderService
{
    private readonly AppDbContext appDbContext;
    private readonly ISep724Provider sep724Provider;
    private readonly IServiceProvider serviceProvider;

    public OrderService(AppDbContext appDbContext,
        ISep724Provider sep724Provider,
        IServiceProvider serviceProvider)
    {
        this.appDbContext = appDbContext;
        this.sep724Provider = sep724Provider;
        this.serviceProvider = serviceProvider;
    }

    public async Task<ServiceResponse<CreateOrderResponseDto>> CreateOrderAsync(CreateOrderRequestDto requestDto, int userId)
    {
        try
        {
            var order = requestDto.Map();
            var person = await appDbContext.People.Select(t => new { t.UserId, CustomerId = t.Customer.Id }).FirstOrDefaultAsync(t => t.UserId == userId);
            if (person == null)
            {
                return new ServiceResponse<CreateOrderResponseDto>
                {
                    Data = null,
                    Succuss = false,
                    Message = "No Custmer"
                };
            }
            order.CustomerId = person.CustomerId;

            await appDbContext.AddAsync(order);
            await appDbContext.SaveChangesAsync();


            return new ServiceResponse<CreateOrderResponseDto>
            {
                Data = new CreateOrderResponseDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                },
                Succuss = true
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<CreateOrderResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ServiceResponse<IList<OrderInqueryOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId)
    {
        try
        {
            var order = await appDbContext.Orders.Include(t => t.OrderParcels).FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);
            if (order is null)
            {
                return new ServiceResponse<IList<OrderInqueryOptionsDto>>
                {
                    Data = null,
                    Succuss = false,
                    Message = "Invalid OrderId"
                };
            }

            var postalServiceBase = typeof(PostalServiceBase);
            var postalProviders = Assembly.GetAssembly(postalServiceBase).GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && postalServiceBase.IsAssignableFrom(t)).ToList();

            var lst = new List<OrderInqueryOptionsDto>();
            var dataMapper = appDbContext.StaticDataMappers
                .Where(t => t.StaticDataId == order.OriginCity || t.StaticDataId == order.DestinationCity
                    || t.StaticDataId == order.DestinationCountry);
            foreach (var postalProvider in postalProviders)
            {
                var constructor = postalProvider.GetConstructors().FirstOrDefault();
                var parameters = constructor.GetParameters();
                var parameterInstances = parameters.Select(param => serviceProvider.GetService(param.ParameterType)).ToArray();

                var instance = (PostalServiceBase)Activator.CreateInstance(postalProvider, parameterInstances);
                var options = await instance.GetOrderInquiryAsync(new OrderInquiryRequestDto
                {
                    CountryId = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.DestinationCountry))?.Code, //todo check providerType
                    OriginCity = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.OriginCity))?.Code,
                    DestinationCity = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.DestinationCity))?.Code,
                    Parcels = order.OrderParcels.Select(t => new OrderInquiryParcelDto
                    {
                        Height = (int)t.Height, //todo
                        Weight = ((int)t.Weight).ToString(),
                        Width = (int)t.Width,
                        Length = (int)t.Length,
                        Value = (t.ParcelValue/10).ToString(),
                        ParcelType = Enum.GetName(typeof(ParcelType), t.ParcelTypeId).ToUpper(),
                        NeedPacking = t.PackageTypeId == 0 ? "0" : "1",
                        LockerSupport = "0"
                    }).ToList(),
                });

                //todo add to database
                if (options is not null)
                {
                    var serviceProvider = await appDbContext.ServiceProviders.FirstOrDefaultAsync(t => t.Name == postalProvider.Name);
                    appDbContext.AddRange(options.Select(t => new OrderInqueryOption
                    {
                        ServiceAmount = decimal.Parse(t.SpecialService, CultureInfo.InvariantCulture),
                        Tax = decimal.Parse(t.Tax, CultureInfo.InvariantCulture),
                        PackagingAmount = decimal.Parse(t.PackageService, CultureInfo.InvariantCulture),
                        FinalAmount = decimal.Parse(t.FinalAmount, CultureInfo.InvariantCulture),
                        OrderId = requestDto.OrderId,
                        ServiceProviderId = serviceProvider.Id,
                        Choosen = false,
                        ServiceType = 0,
                        ServiceSLA = 0,
                        InsuranceAmount = 0,
                        DelivereDate = t.DelivereDate
                    }));
                    await appDbContext.SaveChangesAsync();


                }

            }

            lst = await appDbContext.OrderInqueryOptions.Include(t => t.ServiceProvider).Where(t => t.OrderId == requestDto.OrderId).Select(t =>
            new OrderInqueryOptionsDto
            {
                Id = t.Id,
                BaseAmount = t.ServiceAmount,
                ProviderName = t.ServiceProvider.Title,
                DelivereDate = t.DelivereDate,
                PackageAmount = t.PackagingAmount,
                TaxAmount = t.Tax,
                TotalAmount = t.FinalAmount,
            }).ToListAsync();

            return new ServiceResponse<IList<OrderInqueryOptionsDto>>
            {
                Data = lst,
                Succuss = true,
                Message = string.Empty
            };
        }
        catch (Exception exc)
        {
            return new ServiceResponse<IList<OrderInqueryOptionsDto>>
            {
                Data = null,
                Succuss = false,
                Message = "عدم دریافت سرویس از سرویس دهنده"
            };
        }
    }

    public async Task<ServiceResponse<IList<OrderDto>>> GetOrderListAsync(GetOrderListRequestDto requestDto)
    {
        var customer = await appDbContext.Customers.FirstOrDefaultAsync(t => t.Person.UserId == requestDto.UserId);

        if (customer == null)
        {
            return new ServiceResponse<IList<OrderDto>>
            {
                Data = null,
                Succuss = false,
                Message = "There is no customer"
            };
        }
        var orders = appDbContext.Orders.Where(t => t.CustomerId == customer.Id);
        var query = orders.ApplyQueryParams(requestDto.QueryParams);
        //ToDo add filter & sort
        var result = new List<OrderDto>();
        foreach (var order in query)
        {
            result.Add(order.Map());
        }

        return new ServiceResponse<IList<OrderDto>>
        {
            Data = result,
            Succuss = true,
            Message = string.Empty
        };
    }

    public async Task<ServiceResponse<SelectOptionResponseDto>> SelectOptionAndPaymentAsync(SelectOptionRequestDto requestDto, int userId)
    {
        var person = await appDbContext.People.FirstOrDefaultAsync(t => t.UserId == userId);
        if (person is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = "User is not registered"
            };
        }

        var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);

        if (order is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = "Invalid OrderId"
            };
        }

        var orderInquiry = await appDbContext.OrderInqueryOptions.FirstOrDefaultAsync(t => t.Id == requestDto.OrderInqueryOptionId && t.OrderId == order.Id);
        if (orderInquiry is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = "Invalid OrderInqueryOptionId"
            };
        }

        var result = await sep724Provider.GetPaymentTokenAsync(new PaymentTokenRequestDto
        {
            Amount = orderInquiry.FinalAmount,
            PhoneNumber = person.PhoneNumber,
            Channel = requestDto.Channel,
            CallbackUrl = requestDto.CallbackUrl,
            OrderNumber = order.OrderNumber,
        });
        var data = result?.Data;

        if (result is null || data is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = result?.Title ?? "Error"
            };
        }

        return new ServiceResponse<SelectOptionResponseDto>
        {
            Data = new SelectOptionResponseDto
            {
                ReferenceCode = data.ReferenceCode,
                RequestId = data.RequestId,
                PaymentLink = data.PaymentLink
            },
            Succuss = true,
            Message = result.Message ?? string.Empty,
        };
    }
}
