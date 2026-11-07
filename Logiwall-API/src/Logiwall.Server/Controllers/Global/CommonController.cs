using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Infrastructure.Services.OrderItems;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
using Logiwall.Shared.Extensions;

namespace Logiwall.Server.Controllers.Global;

[Route("api/[controller]/[action]")]
[ApiController]
[AllowAnonymous]
public partial class CommonController : AppControllerBase
{
    [AutoInject] private ICommonService commonService = default!;

    [HttpGet]
    public ServiceResponse<IReadOnlyList<PaymentMethodResponseDto>> GetPaymentMethods() => new()
    {
        Data = new PaymentMethodResponseDto[] { new((int)Constants.PaymentMethod.Online, Localizer["ShippingPaymentOnline"]),
            new((int)Constants.PaymentMethod.CashOnDelivery, Localizer["ShippingPaymentCollect"]),
            new((int)Constants.PaymentMethod.RecipientPayOnDelivery, Localizer["ShippingPaymentRecipientOnDelivery"]),
            new((int)Constants.PaymentMethod.SenderCreditOnDelivery, Localizer["ShippingPaymentSenderCreditOnDelivery"]),
            new((int)Constants.PaymentMethod.SenderWallet, Localizer["ShippingPaymentSenderWallet"]),
            new((int)Constants.PaymentMethod.SenderWalletOnDelivery, Localizer["ShippingPaymentSenderWalletOnDelivery"]),
            new((int)Constants.PaymentMethod.SenderCash, Localizer["ShippingPaymentSenderCash"]),
            new((int)Constants.PaymentMethod.AgencyCredit, Localizer["ShippingPaymentAgencyCredit"]) }
            .Where(m => Constants.SelectablePaymentMethods.Contains((Constants.PaymentMethod)m.Id)).ToArray(),
        Succuss = true
    };

    [HttpGet]
    public ServiceResponse<IReadOnlyList<OrderStatusResponseDto>> GetOrderStatuses() => new()
    {
        Data = Enum.GetValues<OrderStatusCode>().Select(s => new OrderStatusResponseDto((short)s, s.ToString(), s.GetDescription())).ToArray()
    };

    [HttpGet]
    public async Task<ActionResult> GetStaticDatas([FromQuery] GetStaticDataRequestDto requestDto, CancellationToken cancellationToken)
    {
        var result = await commonService.GetStaticDatasAsync(requestDto);
        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }

}
