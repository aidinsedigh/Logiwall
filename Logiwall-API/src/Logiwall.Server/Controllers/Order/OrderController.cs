using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Server.Controllers.Order;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class OrderController : AppControllerBase
{
    [AutoInject] private IOrderService orderService = default!;
    [HttpPost]
    public async Task<ActionResult> CreateOrder(CreateOrderRequestDto requestDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        //if (requestDto.OrderParcels.Any(t => t.Height > 45 || t.Width > 45 || t.Length > 45
        //        || t.Height < 10 || t.Width < 10 || t.Length < 10))
        //{
        //    return BadRequest(AppStrings.LimitedPackageSize);
        //}

        var result = await orderService.CreateOrderAsync(requestDto, userId);

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult> GetOptions([FromQuery]GetOptionnsRequestDto requestDto, CancellationToken cancellationToken)
    {
        var result = await orderService.GetOptionsAsync(requestDto, User.GetUserId());
        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> SelectOption(SelectOptionRequestDto requestDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await orderService.SelectOptionAsync(requestDto, userId);

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> SelectPaymentType(SelectPaymentTypeRequestDto requestDto, CancellationToken cancellationToken)
    {
        var result = await orderService.SelectPaymentTypeAsync(requestDto, User.GetUserId());

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }


    [HttpPost]
    public async Task<ActionResult> GetOrders(GetListQueryParams queryParams)
    {
        var userId = User.GetUserId();

        var result = await orderService.GetOrderListAsync(new GetOrderListRequestDto {
            UserId = userId,
            QueryParams = queryParams
        });

        if (!result.Succuss)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return BadRequest(result);
        }
        return Ok(result);
    }
}
