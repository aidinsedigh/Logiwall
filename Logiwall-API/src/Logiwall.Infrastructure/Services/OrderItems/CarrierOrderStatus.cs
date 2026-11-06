using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Logiwall.Shared.Extensions;

namespace Logiwall.Infrastructure.Services.OrderItems;

internal static class CarrierOrderStatus
{
    public static void Record(AppDbContext db, Order order, DateTimeOffset at)
        => db.OrderStatuses.Add(new OrderStatus { Order = order, Status = (int)order.OverallStatus,
            MappedOrderStatus = order.OverallStatus, Description = order.OverallStatus.GetDescription(), CreationDate = at,
            ServiceProviderId = order.ServiceProviderId, CityId = order.OriginCity });

    public static async Task ApplyAsync(AppDbContext db, Shipment shipment, string? code, string? title,
        DateTimeOffset at, OrderStatusCode fallback, CancellationToken ct)
    {
        var reference = code is null ? null : await db.ProviderReferenceItems.AsNoTracking()
            .SingleOrDefaultAsync(r => r.ServiceProviderId == shipment.ServiceProviderId && r.Category == "status" && r.ExternalCode == code, ct);
        foreach (var parcel in shipment.Parcels)
        {
            parcel.ExternalStatusCode = code;
            parcel.StatusTitle = title ?? reference?.Title;
            parcel.MappedOrderStatus = code is null ? fallback : reference?.MappedOrderStatus;
            parcel.LastStatusAt = at;
        }
        shipment.Order.OverallStatus = OrderStatusAggregation.FromParcels(shipment.Parcels.Select(p => p.MappedOrderStatus), fallback);
    }
}
