using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.PaymentItems;

namespace Logiwall.Application.Abstractions.OrderItems;
public interface IPaymentService
{
    Task<ServiceResponse<PaymentResponseDto>> PaymentOrderStatusAsync(PaymentRequestDto requestDto, int userId);
}
