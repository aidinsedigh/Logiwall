using Logiwall.Shared.Dtos.PaymentItems;
using Logiwall.Client.Core.Controllers.Order;
using Logiwall.Application.Abstractions.OrderItems;

namespace Logiwall.Server.Controllers.Order;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class PaymentController : AppControllerBase, IPaymentController
{
    [AutoInject] private IPaymentService paymentService = default!;

    [HttpPost]
    public async Task<ActionResult> UpdatePaymentOrderStatus(PaymentRequestDto requestDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await paymentService.PaymentOrderStatusAsync(requestDto, userId);

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }
}
