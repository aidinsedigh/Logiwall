using Logiwall.Shared.Dtos.People;

namespace Logiwall.Client.Core.Controllers.People;

[Route("api/[controller]/[action]/")]
public interface IPersonController : IAppController
{
    //[HttpPost]
    //Task<ActionResult> Register(RegisterPersonRequestDto requestDto, CancellationToken cancellationToken);
}
