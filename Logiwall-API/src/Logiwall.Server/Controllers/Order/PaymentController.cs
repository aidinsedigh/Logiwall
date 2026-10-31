using Logiwall.Shared.Dtos.PaymentItems;
using Logiwall.Application.Abstractions.OrderItems;

namespace Logiwall.Server.Controllers.Order;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class PaymentController : AppControllerBase
{
    [AutoInject] private IPaymentService paymentService = default!;

    [HttpPost]
    public async Task<ActionResult> VerifyPayment(VerifyPaymentRequestDto requestDto, CancellationToken cancellationToken)
    {
        var result = await paymentService.VerifyPaymentAsync(requestDto, User.GetUserId(), cancellationToken);
        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }
}
