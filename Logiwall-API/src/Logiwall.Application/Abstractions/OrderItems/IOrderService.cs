using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Application.Abstractions.OrderItems;
public interface IOrderService
{
    Task<ServiceResponse<CreateOrderResponseDto>> CreateOrderAsync(CreateOrderRequestDto requestDto, int userId);

    Task<ServiceResponse<IList<OrderInqueryOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId);

    Task<ServiceResponse<SelectOptionResponseDto>> SelectOptionAndPaymentAsync(SelectOptionRequestDto requestDto, int userId);

    Task<ServiceResponse<IList<OrderDto>>> GetOrderListAsync(GetOrderListRequestDto requestDto);
}
