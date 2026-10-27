using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.PaymentItems;

namespace Logiwall.Application.Abstractions.OrderItems;
public interface IPaymentService
{
    /// <summary>Asks the gateway for the outcome of the order's latest payment. The client never reports a status.</summary>
    Task<ServiceResponse<PaymentResponseDto>> VerifyPaymentAsync(VerifyPaymentRequestDto requestDto, int userId, CancellationToken cancellationToken);
}
