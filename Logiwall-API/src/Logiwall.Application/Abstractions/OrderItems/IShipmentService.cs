using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Application.Abstractions.OrderItems;

public interface IShipmentService
{
    Task<ShipmentResponseDto> RegisterAsync(int orderId, int userId, CancellationToken cancellationToken);
    Task<ShipmentResponseDto> GetAsync(int orderId, int userId, CancellationToken cancellationToken);
    Task<ShipmentResponseDto> CancelAsync(int orderId, int userId, CancellationToken cancellationToken);
    Task<ParcelTrackingResponseDto> TrackAsync(TrackParcelRequestDto request, CancellationToken cancellationToken);
    Task ReceiveEventAsync(CarrierParcelEventRequestDto request, CancellationToken cancellationToken);
}
