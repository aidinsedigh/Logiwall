using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Infrastructure.Data;
using Logiwall.Shared.Dtos.Dashboard;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
using Logiwall.Shared.Dtos.Providers.Sep724;
using Logiwall.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Logiwall.Infrastructure.Options;
using Microsoft.Extensions.Options;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Infrastructure.Services.OrderItems;
public partial class OrderService(AppDbContext appDbContext,
    ISep724Provider sep724Provider,
    ILogger<OrderService> logger,
    IOptions<PaymentOptions> paymentOptions,
    IStringLocalizer<AppStrings> localizer,
    ICarrierQuoteService carrierQuoteService) : IOrderService
{
    private readonly PaymentOptions paymentOptions = paymentOptions.Value;

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
            CarrierOrderStatus.Record(appDbContext, order, DateTimeOffset.UtcNow);
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

    public Task<ServiceResponse<IList<OrderProviderOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId, CancellationToken cancellationToken = default)
        => carrierQuoteService.GetAsync(requestDto.OrderId, userId, cancellationToken, requestDto.PaymentType, requestDto.ServiceReferenceId);

    /// <summary>The only order properties a client may filter or sort on, by client name.</summary>
    private static readonly Dictionary<string, string> OrderFilterableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = nameof(Order.Id),
        ["originCity"] = nameof(Order.OriginCity),
        ["destinationCity"] = nameof(Order.DestinationCity),
        ["status"] = nameof(Order.OverallStatus),
        ["state"] = nameof(Order.OverallStatus),
        ["orderNumber"] = nameof(Order.OrderNumber),
        ["creationDate"] = nameof(Order.CreationDate),
    };

    /// <summary>An unpaged list returns at most this many orders, the newest first.</summary>
    private const int UnpagedOrderLimit = 100;

    public async Task<ServiceResponse<OrderListResponseDto>> GetOrderListAsync(GetOrderListRequestDto requestDto)
    {
        var customerId = await appDbContext.Customers.Where(t => t.Person.UserId == requestDto.UserId).Select(t => (int?)t.Id).FirstOrDefaultAsync();

        if (customerId == null)
        {
            return new ServiceResponse<OrderListResponseDto>
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

        var totalCount = await query.CountAsync();

        // Paging needs a stable order, so pages never overlap or skip rows.
        if (query.Expression.Type != typeof(IOrderedQueryable<Order>))
        {
            query = query.OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id);
        }
        else
            query = ((IOrderedQueryable<Order>)query).ThenByDescending(t => t.Id);

        query = queryParams.HasPagination
            ? query.ApplyPagination(queryParams.PageNumber, queryParams.PageSize)
            : query.Take(UnpagedOrderLimit);

        var rows = await query.Select(o => new
        {
            Order = o,
            ServiceProviderName = appDbContext.ServiceProviders.Where(p => p.Id == o.ServiceProviderId)
                .Select(p => p.Title).FirstOrDefault(),
            // shortcut: the card shows one code, so a multi-parcel order shows its first parcel's code; the shipment panel lists all.
            TrackingCode = o.ServiceProviderTrackingCode ?? appDbContext.ShipmentParcels
                .Where(p => p.Shipment.OrderId == o.Id && p.TrackingCode != null)
                .OrderBy(p => p.Position).Select(p => p.TrackingCode).FirstOrDefault(),
            // Orders has no FinalAmount column: the chosen quote holds the total.
            FinalAmount = o.OrderInqueryOptions.Where(t => t.Choosen).Select(t => t.FinalAmount).FirstOrDefault(),
            PaidAmount = o.OrderPayments.Where(p => p.Status == PaymentStatus.Success && p.VerifiedAt != null && p.VerifiedAmount != null)
                .Sum(p => (decimal?)p.VerifiedAmount) ?? 0m,
            HasPendingPayment = o.OrderPayments.Any(p => p.Status == PaymentStatus.Pending
                || p.Status == PaymentStatus.Success && (p.VerifiedAt == null || p.VerifiedAmount == null)),
            HasFailedPayment = o.OrderPayments.Any(p => p.Status == PaymentStatus.UnSuccess),
            OriginCityName = appDbContext.StaticDatas.Where(s => s.Id == o.OriginCity).Select(s => s.Title).FirstOrDefault(),
            DestinationCityName = appDbContext.StaticDatas.Where(s => s.Id == o.DestinationCity).Select(s => s.Title).FirstOrDefault(),
        }).ToListAsync();

        var result = rows.Select(t =>
        {
            var payment = t.PaidAmount > 0 ? (t.FinalAmount > t.PaidAmount ? OrderPaymentState.PartiallyPaid : OrderPaymentState.Paid)
                : t.HasPendingPayment ? OrderPaymentState.Unpaid
                : t.HasFailedPayment ? OrderPaymentState.Failed : OrderPaymentState.Unpaid;
            return t.Order.Map() with
            {
            FinalAmount = t.FinalAmount,
            ServiceProviderName = t.ServiceProviderName,
            ServiceProviderTrackingCode = t.TrackingCode,
            PaymentStatus = payment.ToString(),
            PaymentStatusName = payment.GetDescription(),
            StatusName = t.Order.OverallStatus.GetDescription(),
            OriginCityName = t.OriginCityName,
            DestinationCityName = t.DestinationCityName,
            };
        }).ToList();

        return new ServiceResponse<OrderListResponseDto>
        {
            Data = new OrderListResponseDto(totalCount, result),
            Succuss = true,
            Message = string.Empty
        };
    }

    public async Task<ServiceResponse<OverallAnalyticsStatsDataResponseDto>> GetOrderStatsAsync(CancellationToken cancellationToken)
    {
        // Business-wide counts for the admin panel (ADR-0005). Only admins may call this.
        var orders = appDbContext.Orders.AsNoTracking();
        var since = DateTimeOffset.UtcNow.AddDays(-30);

        return new ServiceResponse<OverallAnalyticsStatsDataResponseDto>
        {
            Data = new()
            {
                TotalOrders = await orders.CountAsync(cancellationToken),
                Last30DaysOrders = await orders.CountAsync(t => t.CreationDate >= since, cancellationToken),
                PaidOrders = await orders.CountAsync(t => t.OrderPayments.Any(p => p.Status == PaymentStatus.Success), cancellationToken),
            }
        };
    }

    public async Task<ServiceResponse<SelectOptionResponseDto>> SelectOptionAsync(SelectOptionRequestDto requestDto, int userId)
    {
        await using var selectionTx = await appDbContext.Database.BeginTransactionAsync();
        await appDbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={requestDto.OrderId}");
        var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);
        if (order is null)
            return Fail<SelectOptionResponseDto>(nameof(AppStrings.InvalidOrder));

        var orderInquiry = await appDbContext.OrderInqueryOptions.FirstOrDefaultAsync(t => t.Id == requestDto.OrderInqueryOptionId && t.OrderId == order.Id);
        if (orderInquiry is null)
            return Fail<SelectOptionResponseDto>(nameof(AppStrings.InvalidOrderOption));
        if (await OrderDraftWorkflow.IsLockedAsync(appDbContext, order.Id)
            || orderInquiry.CarrierRequestJson is null && order.State is (short)OrderState.Paid or (short)OrderState.PaymentPending)
            return Fail<SelectOptionResponseDto>("ShippingOrderLocked");
        if (orderInquiry.CarrierRequestJson is not null && (orderInquiry.ExpiresAt is null || orderInquiry.ExpiresAt <= DateTimeOffset.UtcNow))
            return Fail<SelectOptionResponseDto>("ShippingQuoteExpired");


        // Record the accepted quote, so SelectPaymentType charges what the customer chose.
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
        order.OverallStatus = OrderStatusCode.Draft;
        order.ModificationDate = DateTimeOffset.Now;
        appDbContext.OrderStatuses.Add(new OrderStatus
        {
            OrderId = order.Id,
            Status = (int)OrderState.SelectedServiceProvider,
            MappedOrderStatus = OrderStatusCode.Draft,
            ServiceProviderId = orderInquiry.ServiceProviderId,
            CityId = order.OriginCity,
            Description = "Service option selected"
        });
        await appDbContext.SaveChangesAsync();

        await selectionTx.CommitAsync();
        return new ServiceResponse<SelectOptionResponseDto>
        {
            Data = new SelectOptionResponseDto(
            [
                new PickupOptionDto(orderInquiry.Id, null, orderInquiry.FinalAmount, orderInquiry.ServiceAmount, orderInquiry.PackagingAmount, orderInquiry.Tax, string.Empty, orderInquiry.PaymentMethod)
            ]),
            Succuss = true,
            Message = string.Empty
        };
    }

    public async Task<ServiceResponse<SelectPaymentTypeResponseDto>> SelectPaymentTypeAsync(SelectPaymentTypeRequestDto requestDto, int userId)
    {
        // The front settles cash on delivery itself and calls this endpoint only for online payment.
        if (requestDto.PaymentType != PaymentMethod.Online)
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.PaymentTypeNotSupported));

        var person = await appDbContext.People.FirstOrDefaultAsync(t => t.UserId == userId);
        if (person is null)
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.UserIsNotRegistered));

        await using var paymentTx = await appDbContext.Database.BeginTransactionAsync();
        await appDbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={requestDto.OrderId}");
        var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);
        if (order is null)
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.InvalidOrder));

        if (order.State == (short)OrderState.Paid || await OrderDraftWorkflow.IsLockedAsync(appDbContext, order.Id)
            || await appDbContext.OrderPayments.AnyAsync(p => p.OrderId == order.Id && p.Status == PaymentStatus.Success && p.VerifiedAt != null))
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.OrderIsAlreadyPaid));

        // Only the option SelectOption recorded can be paid.
        var orderInquiry = await appDbContext.OrderInqueryOptions.FirstOrDefaultAsync(t => t.Id == requestDto.PickupOptionId && t.OrderId == order.Id && t.Choosen);
        if (orderInquiry is null)
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.InvalidOrderOption));
        if (orderInquiry.PaymentMethod != requestDto.PaymentType)
            return Fail<SelectPaymentTypeResponseDto>("ShippingPaymentMethodMismatch");
        if (orderInquiry.CarrierRequestJson is not null && (orderInquiry.ExpiresAt is null || orderInquiry.ExpiresAt <= DateTimeOffset.UtcNow))
            return Fail<SelectPaymentTypeResponseDto>("ShippingQuoteExpired");

        // The gateway redirects the customer to this URL, so it must point at our own frontend.
        if (Uri.TryCreate(requestDto.CallbackUrl, UriKind.Absolute, out var callbackUri) is false)
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.InvalidCallbackUrl));

        if (paymentOptions.AllowedCallbackOrigins.Length == 0)
        {
            logger.LogWarning("[Order][SelectPaymentType][{OrderId}] Payment:AllowedCallbackOrigins is empty; any callback URL is accepted.", order.Id);
        }
        else if (paymentOptions.AllowedCallbackOrigins.Contains(callbackUri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase) is false)
        {
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.InvalidCallbackUrl));
        }

        // TODO(plan 010): unit unconfirmed (ADR-0001 verdict c), so the quote goes to the gateway unconverted.
        var expectedGatewayAmount = (long)decimal.Round(orderInquiry.FinalAmount, MidpointRounding.AwayFromZero);

        var result = await sep724Provider.GetPaymentTokenAsync(new PaymentTokenRequestDto
        {
            Amount = expectedGatewayAmount,
            PhoneNumber = person.PhoneNumber,
            Channel = string.IsNullOrEmpty(requestDto.Channel) ? "pwa" : requestDto.Channel,
            CallbackUrl = requestDto.CallbackUrl,
            OrderNumber = order.OrderNumber,
        });
        var data = result?.Data;

        if (result is null || data is null)
        {
            // Provider text goes to the log only (ADR-0003).
            logger.LogWarning("[Order][SelectPaymentType][{OrderId}][{ProviderMessage}] Payment link was not created.", order.Id, result?.Title ?? result?.Message);
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.PaymentLinkFailed));
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

        try
        {
            await appDbContext.SaveChangesAsync();
            await paymentTx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            // A repeated gateway reference code breaks the unique index; the link cannot be tracked, so it is not given out.
            logger.LogError(ex, "[Order][SelectPaymentType][{OrderId}] Payment was not saved.", order.Id);
            return Fail<SelectPaymentTypeResponseDto>(nameof(AppStrings.PaymentLinkFailed));
        }

        return new ServiceResponse<SelectPaymentTypeResponseDto>
        {
            Data = new SelectPaymentTypeResponseDto
            {
                ReferenceCode = data.ReferenceCode,
                RequestId = data.RequestId,
                PaymentLink = data.PaymentLink
            },
            Succuss = true,
            Message = localizer[nameof(AppStrings.PaymentLinkCreated)],
        };
    }

    private ServiceResponse<T> Fail<T>(string messageKey) => new() { Data = default, Succuss = false, Message = localizer[messageKey] };
}
