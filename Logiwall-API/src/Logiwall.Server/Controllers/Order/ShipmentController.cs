using Logiwall.Application.Abstractions.Global;
using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Server.Services;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Server.Controllers.Order;

[ApiController, Route("api/[controller]/[action]")]
public partial class ShipmentController : AppControllerBase
{
    [AutoInject] private IShipmentService shipments = default!;
    [AutoInject] private IShippingReferenceService references = default!;

    [HttpPost]
    public async Task<ServiceResponse<ShipmentResponseDto>> Register(RegisterShipmentRequestDto request, CancellationToken cancellationToken)
        => new() { Succuss = true, Data = await shipments.RegisterAsync(request.OrderId, User.GetUserId(), cancellationToken) };

    [HttpGet]
    public async Task<ServiceResponse<ShipmentResponseDto>> Get([FromQuery] int orderId, CancellationToken cancellationToken)
        => new() { Succuss = true, Data = await shipments.GetAsync(orderId, User.GetUserId(), cancellationToken) };

    [HttpPost]
    public async Task<ServiceResponse<ShipmentResponseDto>> Cancel(RegisterShipmentRequestDto request, CancellationToken cancellationToken)
        => new() { Succuss = true, Data = await shipments.CancelAsync(request.OrderId, User.GetUserId(), cancellationToken) };

    [HttpGet]
    public async Task<ServiceResponse<IReadOnlyList<ShippingReferenceResponseDto>>> References(
        [FromQuery] string category, [FromQuery] ShippingReferenceFilterDto filter, CancellationToken cancellationToken,
        [FromQuery] string providerCode = "tipax")
        => new() { Succuss = true, Data = await references.GetAsync(providerCode, category, cancellationToken, filter) };

    [HttpPost, Authorize(Policy = "AdminsOnly")]
    public async Task<ServiceResponse<bool>> SynchronizeReferences(CancellationToken cancellationToken)
    {
        await references.SynchronizeAsync(cancellationToken);
        return new() { Succuss = true, Data = true };
    }

    [HttpGet]
    public async Task<ServiceResponse<IReadOnlyList<ShippingReferenceResponseDto>>> Services(
        CancellationToken cancellationToken, [FromQuery] string providerCode = "tipax")
        => new() { Succuss = true, Data = await references.GetAsync(providerCode, "service", cancellationToken) };


    // A customer can track only the parcels of their own shipments.
    [HttpGet, Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parcel-tracking")]
    public async Task<ServiceResponse<ParcelTrackingResponseDto>> Track([FromQuery] TrackParcelRequestDto request, CancellationToken cancellationToken)
        => new() { Succuss = true, Data = await shipments.TrackAsync(request, User.GetUserId(), cancellationToken) };

    [HttpPost, Authorize(AuthenticationSchemes = ShippingWebhookAuthentication.SchemeName)]
    public async Task<IActionResult> Webhook(CarrierParcelEventRequestDto request, CancellationToken cancellationToken)
    {
        await shipments.ReceiveEventAsync(request, cancellationToken);
        return Ok(new { received = true });
    }
}
