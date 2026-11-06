using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.OrderItems;

internal static class OrderDraftWorkflow
{
    public static Task<bool> IsLockedAsync(AppDbContext db, int orderId, CancellationToken ct = default)
        => db.Shipments.AnyAsync(s => s.OrderId == orderId &&
            (s.RegisteredAt != null || s.ExternalOrderId != null ||
             s.State != ShipmentRegistrationState.Rejected && s.State != ShipmentRegistrationState.Prepared), ct);

    public static void ClearSelection(Order order)
    {
        order.ServiceAmount = 0;
        order.PackagingAmount = 0;
        order.InsuranceAmount = 0;
        order.Tax = 0;
        order.ServiceProviderId = 0;
        order.ServiceType = 0;
        order.ServiceSLA = 0;
        order.PickupServiceId = 0;
        order.PickupDateTime = null;
        order.ServiceProviderTrackingCode = null;
        order.State = (short)OrderState.Created;
        order.OverallStatus = OrderStatusCode.Draft;
        var now = DateTimeOffset.UtcNow;
        order.ModificationDate = order.ModificationDate is { } previous && now <= previous ? previous.AddTicks(1) : now;
    }
}


