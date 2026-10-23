using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Application.Abstractions.Providers.PaymentGateways;
using Logiwall.Shared.Dtos.PaymentItems;
using Logiwall.Shared.Dtos.Providers.PostalService;

namespace Logiwall.Infrastructure.Providers.PostalServices;

//todo should be abstract
public abstract class PostalServiceBase : IPostalServiceBase
{
    protected PostalServiceBase(IAppSettingService appSettingService,
        IHttpProvider httpProvider)
    {
        AppSettingService = appSettingService;
        HttpProvider = httpProvider;
    }

    protected IAppSettingService AppSettingService { get; }
    protected IHttpProvider HttpProvider { get; }

    public abstract Task<List<OrderInquiryOptionDto>> GetOrderInquiryAsync(OrderInquiryRequestDto requestDto);

    public PaymentResponseDto Payment(PaymentRequestDto requestDto)
    {
        return new PaymentResponseDto
        {
            OrderId = requestDto.OrderId,
            Status = PaymentStatus.Success,
            Message = string.Empty,
        };
    }
}
