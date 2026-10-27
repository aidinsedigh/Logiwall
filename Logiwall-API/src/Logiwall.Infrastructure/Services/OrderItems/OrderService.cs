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
using Microsoft.Extensions.Logging;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Infrastructure.Services.OrderItems;
public class OrderService : IOrderService
{
    private readonly AppDbContext appDbContext;
    private readonly ISep724Provider sep724Provider;
    private readonly IEnumerable<PostalServiceBase> postalProviders;
    private readonly ILogger<OrderService> logger;
    private readonly PaymentOptions paymentOptions;

    public OrderService(AppDbContext appDbContext,
        ISep724Provider sep724Provider,
        IEnumerable<PostalServiceBase> postalProviders,
        ILogger<OrderService> logger,
        IOptions<PaymentOptions> paymentOptions)
    {
        this.appDbContext = appDbContext;
        this.sep724Provider = sep724Provider;
        this.postalProviders = postalProviders;
        this.logger = logger;
        this.paymentOptions = paymentOptions.Value;
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
            logger.LogError(ex, "CreateOrderAsync failed");
            return new ServiceResponse<CreateOrderResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = "An unexpected error occurred."
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

            if (order.State is (short)OrderState.Paid or (short)OrderState.PaymentPending)
            {
                return new ServiceResponse<IList<OrderInqueryOptionsDto>>
                {
                    Data = null,
                    Succuss = false,
                    Message = "Order can no longer be re-quoted."
                };
            }

            var staticDataIds = new int[] { order.OriginCity, order.DestinationCity, order.DestinationCountry ?? -1 };
            var mapperCodes = await appDbContext.StaticDataMappers
                .Where(t => staticDataIds.Contains(t.StaticDataId))
                .Select(t => new { t.StaticDataId, t.ProviderType, t.Code })
                .ToListAsync();

            var newOptions = new List<OrderInqueryOption>();
            var anyProviderSucceeded = false;
            foreach (var provider in postalProviders)
            {
                string? CodeFor(int? staticDataId) => mapperCodes
                    .FirstOrDefault(t => t.StaticDataId == staticDataId && t.ProviderType == provider.ProviderType)?.Code;

                List<OrderInquiryOptionDto>? options;
                try
                {
                    options = await provider.GetOrderInquiryAsync(new OrderInquiryRequestDto
                    {
                        CountryId = CodeFor(order.DestinationCountry),
                        OriginCity = CodeFor(order.OriginCity),
                        DestinationCity = CodeFor(order.DestinationCity),
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
                }
                catch (Exception ex)
                {
                    // One failing carrier must not fail the whole quote.
                    logger.LogWarning(ex, "Quote from {Provider} failed", provider.GetType().Name);
                    continue;
                }

                if (options is null)
                    continue;

                var serviceProvider = await appDbContext.ServiceProviders.FirstOrDefaultAsync(t => t.Name == provider.GetType().Name);
                if (serviceProvider is null)
                {
                    logger.LogWarning("No ServiceProviders row for {Provider}; its options are skipped", provider.GetType().Name);
                    continue;
                }

                anyProviderSucceeded = true;
                newOptions.AddRange(options.Select(t => new OrderInqueryOption
                {
                    ServiceAmount = decimal.Parse(t.SpecialService, CultureInfo.InvariantCulture),
                    Tax = decimal.Parse(t.Tax, CultureInfo.InvariantCulture),
                    PackagingAmount = decimal.Parse(t.PackageService, CultureInfo.InvariantCulture),
                    FinalAmount = decimal.Parse(t.FinalAmount, CultureInfo.InvariantCulture),
                    OrderId = order.Id,
                    ServiceProviderId = serviceProvider.Id,
                    Choosen = false,
                    ServiceType = 0,
                    ServiceSLA = 0,
                    InsuranceAmount = 0,
                    DelivereDate = t.DelivereDate
                }));
            }

            if (anyProviderSucceeded is false)
            {
                // Keep the previous quotes: the customer may still select one of them.
                return new ServiceResponse<IList<OrderInqueryOptionsDto>>
                {
                    Data = null,
                    Succuss = false,
                    Message = "عدم دریافت سرویس از سرویس دهنده"
                };
            }

            // Re-quoting replaces the previous options instead of appending to them.
            await using (var tx = await appDbContext.Database.BeginTransactionAsync())
            {
                var old = await appDbContext.OrderInqueryOptions.Where(t => t.OrderId == order.Id).ToListAsync();
                appDbContext.OrderInqueryOptions.RemoveRange(old);
                appDbContext.OrderInqueryOptions.AddRange(newOptions);
                order.State = (short)OrderState.Created;
                await appDbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }

            var lst = await appDbContext.OrderInqueryOptions.Include(t => t.ServiceProvider).Where(t => t.OrderId == requestDto.OrderId).Select(t =>
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
            logger.LogError(exc, "GetOptionsAsync failed");
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

        if (order.State == (short)OrderState.Paid)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = "Order is already paid."
            };
        }

        // The gateway redirects the customer to this URL, so it must point at our own frontend.
        if (Uri.TryCreate(requestDto.CallbackUrl, UriKind.Absolute, out var callbackUri) is false)
        {
            return new ServiceResponse<SelectOptionResponseDto> { Data = null, Succuss = false, Message = "Invalid callback URL" };
        }
        if (paymentOptions.AllowedCallbackOrigins.Length == 0)
        {
            logger.LogWarning("Payment:AllowedCallbackOrigins is empty; any callback URL is accepted.");
        }
        else if (paymentOptions.AllowedCallbackOrigins.Contains(callbackUri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase) is false)
        {
            return new ServiceResponse<SelectOptionResponseDto> { Data = null, Succuss = false, Message = "Invalid callback URL" };
        }

        // TODO(plan 010): unit unconfirmed (ADR-0001 verdict c), so the quote goes to the gateway unconverted.
        var expectedGatewayAmount = (long)decimal.Round(orderInquiry.FinalAmount, MidpointRounding.AwayFromZero);

        // Record the accepted quote before the gateway call, so later steps know what the customer chose.
        var orderOptions = await appDbContext.OrderInqueryOptions.Where(t => t.OrderId == order.Id).ToListAsync();
        foreach (var option in orderOptions)
            option.Choosen = option.Id == orderInquiry.Id;

        order.ServiceAmount = orderInquiry.ServiceAmount;
        order.PackagingAmount = orderInquiry.PackagingAmount;
        order.InsuranceAmount = orderInquiry.InsuranceAmount;
        order.Tax = orderInquiry.Tax;
        order.ServiceType = orderInquiry.ServiceType;
        order.ServiceSLA = orderInquiry.ServiceSLA;
        order.ServiceProviderId = orderInquiry.ServiceProviderId;
        order.State = (short)OrderState.SelectedServiceProvider;
        order.ModificationDate = DateTimeOffset.Now;
        appDbContext.OrderStatuses.Add(new OrderStatus
        {
            OrderId = order.Id,
            Status = (int)OrderState.SelectedServiceProvider,
            ServiceProviderId = orderInquiry.ServiceProviderId,
            CityId = order.OriginCity,
            Description = "Service option selected"
        });
        await appDbContext.SaveChangesAsync();

        var result = await sep724Provider.GetPaymentTokenAsync(new PaymentTokenRequestDto
        {
            Amount = expectedGatewayAmount,
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

        // The server-computed amount is the only truth; verification later compares the gateway against it.
        appDbContext.OrderPayments.Add(new OrderPayment
        {
            OrderId = order.Id,
            Amount = expectedGatewayAmount,
            PaymentType = PaymentType.BankGateway,
            Status = PaymentStatus.Pending,
            PayTrackingCode = data.RequestId.ToString(CultureInfo.InvariantCulture),
            PayDate = DateTimeOffset.Now,
            GatewayRequestId = data.RequestId,
            GatewayReferenceCode = data.ReferenceCode,
        });
        order.State = (short)OrderState.PaymentPending;
        order.ModificationDate = DateTimeOffset.Now;
        appDbContext.OrderStatuses.Add(new OrderStatus
        {
            OrderId = order.Id,
            Status = (int)OrderState.PaymentPending,
            ServiceProviderId = orderInquiry.ServiceProviderId,
            CityId = order.OriginCity,
            Description = "Payment link issued"
        });
        await appDbContext.SaveChangesAsync();

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
