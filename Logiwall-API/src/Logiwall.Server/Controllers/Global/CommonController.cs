using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Infrastructure.Services.OrderItems;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Server.Controllers.Global;

[Route("api/[controller]/[action]")]
[ApiController]
[AllowAnonymous]
public partial class CommonController : AppControllerBase
{
    [AutoInject] private ICommonService commonService = default!;

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
