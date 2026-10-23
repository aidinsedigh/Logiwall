using Logiwall.Shared.Dtos.PaymentItems;
using Logiwall.Shared.Dtos.Providers.PostalService;

namespace Logiwall.Application.Abstractions.Providers.PaymentGateways;
public interface IPostalServiceBase
{
    PaymentResponseDto Payment(PaymentRequestDto requestDto);

    Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto);
}
