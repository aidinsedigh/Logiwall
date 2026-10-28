using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Infrastructure.Data;
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
    private readonly IStringLocalizer<AppStrings> localizer;

    public OrderService(AppDbContext appDbContext,
        ISep724Provider sep724Provider,
        IEnumerable<PostalServiceBase> postalProviders,
        ILogger<OrderService> logger,
        IOptions<PaymentOptions> paymentOptions,
        IStringLocalizer<AppStrings> localizer)
    {
        this.appDbContext = appDbContext;
        this.sep724Provider = sep724Provider;
        this.postalProviders = postalProviders;
        this.logger = logger;
        this.paymentOptions = paymentOptions.Value;
        this.localizer = localizer;
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
                    Message = localizer[nameof(AppStrings.CustomerNotFound)]
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
            logger.LogError(ex, "[Order][CreateOrder][{UserId}] Order creation failed.", userId);
            return new ServiceResponse<CreateOrderResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.UnknownException)]
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
                    Message = localizer[nameof(AppStrings.InvalidOrder)]
                };
            }

            if (order.State is (short)OrderState.Paid or (short)OrderState.PaymentPending)
            {
                return new ServiceResponse<IList<OrderInqueryOptionsDto>>
                {
                    Data = null,
                    Succuss = false,
                    Message = localizer[nameof(AppStrings.OrderCannotBeRequoted)]
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
                    logger.LogWarning(ex, "[Order][GetOptions][{OrderId}][{Provider}] Quote request failed.", order.Id, provider.GetType().Name);
                    continue;
                }

                if (options is null)
                    continue;

                var serviceProvider = await appDbContext.ServiceProviders.FirstOrDefaultAsync(t => t.Name == provider.GetType().Name);
                if (serviceProvider is null)
                {
                    logger.LogWarning("[Order][GetOptions][{OrderId}][{Provider}] No ServiceProviders row; options skipped.", order.Id, provider.GetType().Name);
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
                    Message = localizer[nameof(AppStrings.ProviderServiceUnavailable)]
                };
            }

            // Re-quoting replaces the previous options instead of appending to them.
            await using (var tx = await appDbContext.Database.BeginTransactionAsync())
            {
                // Lock the order row, so a parallel re-quote waits and then replaces these options.
                await appDbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK, HOLDLOCK) WHERE Id = {order.Id}");
                var old =await appDbContext.OrderInqueryOptions.Where(t => t.OrderId == order.Id).ToListAsync();
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
            logger.LogError(exc, "[Order][GetOptions][{OrderId}] Getting options failed.", requestDto.OrderId);
            return new ServiceResponse<IList<OrderInqueryOptionsDto>>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.ProviderServiceUnavailable)]
            };
        }
    }

    /// <summary>The only order properties a client may filter or sort on, by client name.</summary>
    private static readonly Dictionary<string, string> OrderFilterableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = nameof(Order.Id),
        ["originCity"] = nameof(Order.OriginCity),
        ["destinationCity"] = nameof(Order.DestinationCity),
        ["status"] = nameof(Order.State),
        ["state"] = nameof(Order.State),
        ["orderNumber"] = nameof(Order.OrderNumber),
        ["creationDate"] = nameof(Order.CreationDate),
    };

    /// <summary>An unpaged list returns at most this many orders, the newest first.</summary>
    private const int UnpagedOrderLimit = 100;

    public async Task<ServiceResponse<IList<OrderDto>>> GetOrderListAsync(GetOrderListRequestDto requestDto)
    {
        var customerId = await appDbContext.Customers.Where(t => t.Person.UserId == requestDto.UserId).Select(t => (int?)t.Id).FirstOrDefaultAsync();

        if (customerId == null)
        {
            return new ServiceResponse<IList<OrderDto>>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.CustomerNotFound)]
            };
        }

        var queryParams = requestDto.QueryParams;
        var query = appDbContext.Orders.AsNoTracking().Where(t => t.CustomerId == customerId)
            .ApplyFilters(queryParams.FilterGroups, OrderFilterableProperties)
            .ApplySorting(queryParams.SortConditions, OrderFilterableProperties);

        // Paging needs a stable order, so pages never overlap or skip rows.
        if (query.Expression.Type != typeof(IOrderedQueryable<Order>))
        {
            query = query.OrderByDescending(t => t.Id);
        }

        query = queryParams.HasPagination
            ? query.ApplyPagination(queryParams.PageNumber, queryParams.PageSize)
            : query.Take(UnpagedOrderLimit);

        var result = (await query.ToListAsync()).Select(o => o.Map()).ToList();

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
                Message = localizer[nameof(AppStrings.UserIsNotRegistered)]
            };
        }

        var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);

        if (order is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.InvalidOrder)]
            };
        }

        var orderInquiry = await appDbContext.OrderInqueryOptions.FirstOrDefaultAsync(t => t.Id == requestDto.OrderInqueryOptionId && t.OrderId == order.Id);
        if (orderInquiry is null)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.InvalidOrderOption)]
            };
        }

        if (order.State == (short)OrderState.Paid)
        {
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.OrderIsAlreadyPaid)]
            };
        }

        // The gateway redirects the customer to this URL, so it must point at our own frontend.
        if (Uri.TryCreate(requestDto.CallbackUrl, UriKind.Absolute, out var callbackUri) is false)
        {
            return new ServiceResponse<SelectOptionResponseDto> { Data = null, Succuss = false, Message = localizer[nameof(AppStrings.InvalidCallbackUrl)] };
        }
        if (paymentOptions.AllowedCallbackOrigins.Length == 0)
        {
            logger.LogWarning("[Order][SelectOptionAndPayment][{OrderId}] Payment:AllowedCallbackOrigins is empty; any callback URL is accepted.", order.Id);
        }
        else if (paymentOptions.AllowedCallbackOrigins.Contains(callbackUri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase) is false)
        {
            return new ServiceResponse<SelectOptionResponseDto> { Data = null, Succuss = false, Message = localizer[nameof(AppStrings.InvalidCallbackUrl)] };
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
            // Provider text goes to the log only (ADR-0003).
            logger.LogWarning("[Order][SelectOptionAndPayment][{OrderId}][{ProviderMessage}] Payment link was not created.", order.Id, result?.Title ?? result?.Message);
            return new ServiceResponse<SelectOptionResponseDto>
            {
                Data = null,
                Succuss = false,
                Message = localizer[nameof(AppStrings.PaymentLinkFailed)]
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
            Message = localizer[nameof(AppStrings.PaymentLinkCreated)],
        };
    }
}
