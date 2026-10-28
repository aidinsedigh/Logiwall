using Logiwall.Shared.Dtos.Dashboard;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Application.Abstractions.OrderItems;
public interface IOrderService
{
    Task<ServiceResponse<CreateOrderResponseDto>> CreateOrderAsync(CreateOrderRequestDto requestDto, int userId);

    Task<ServiceResponse<IList<OrderProviderOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId);

    Task<ServiceResponse<SelectOptionResponseDto>> SelectOptionAsync(SelectOptionRequestDto requestDto, int userId);

    Task<ServiceResponse<SelectPaymentTypeResponseDto>> SelectPaymentTypeAsync(SelectPaymentTypeRequestDto requestDto, int userId);

    Task<ServiceResponse<OrderListResponseDto>> GetOrderListAsync(GetOrderListRequestDto requestDto);

    Task<ServiceResponse<OverallAnalyticsStatsDataResponseDto>> GetOrderStatsAsync(CancellationToken cancellationToken);
}
