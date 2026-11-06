using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.OrderItems;

public partial class OrderService
{
    public async Task<ServiceResponse<CreateOrderResponseDto>> UpdateOrderAsync(UpdateOrderRequestDto requestDto, int userId, CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        await using var tx = await appDbContext.Database.BeginTransactionAsync(ct);
        await appDbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Orders WITH (UPDLOCK,HOLDLOCK) WHERE Id={requestDto.OrderId}", ct);
        var order = await appDbContext.Orders.Include(o => o.OrderParcels)
            .SingleOrDefaultAsync(o => o.Id == requestDto.OrderId && o.Customer.Person.UserId == userId, ct)
            ?? throw new ResourceNotFoundException(nameof(AppStrings.InvalidOrder));
        if (await OrderDraftWorkflow.IsLockedAsync(appDbContext, order.Id, ct))
            throw new ConflictException("ShippingOrderLocked");

        var shipment = await appDbContext.Shipments.SingleOrDefaultAsync(s => s.OrderId == order.Id, ct);
        if (shipment is not null)
        {
            // Definitively rejected attempts remain in history, but their parcel links cannot constrain the edited form.
            await appDbContext.ShipmentParcels.Where(p => p.ShipmentId == shipment.Id).ExecuteDeleteAsync(ct);
            shipment.State = ShipmentRegistrationState.Prepared;
            shipment.QuoteId = 0;
            shipment.RequestJson = string.Empty;
            shipment.LastErrorCode = null;
        }
        await appDbContext.OrderInqueryOptions.Where(q => q.OrderId == order.Id).ExecuteDeleteAsync(ct);
        appDbContext.OrderParcels.RemoveRange(order.OrderParcels);
        order.OrderParcels = requestDto.OrderParcels.Select(p => p.MapParcels()).ToList();
        order.ChannelId = requestDto.ChannelId;
        order.SenderName = requestDto.SenderName;
        order.SenderLastName = requestDto.SenderLastName;
        order.SenderPhoneNumber = requestDto.SenderPhoneNumber;
        order.OriginCity = requestDto.OriginCity;
        order.OriginLat = requestDto.OriginLat;
        order.OriginLng = requestDto.OriginLng;
        order.OriginAddress = requestDto.OriginAddress;
        order.OriginPostalCode = requestDto.OriginPostalCode;
        order.OriginalPlaque = requestDto.OriginalPlaque;
        order.OriginalUnit = requestDto.OriginalUnit;
        order.ReceiverName = requestDto.ReceiverName;
        order.ReceiverLastName = requestDto.ReceiverLastName;
        order.ReceiverPhoneNumber = requestDto.ReceiverPhoneNumber;
        order.DestinationCountry = requestDto.DestinationCountry;
        order.DestinationCity = requestDto.DestinationCity;
        order.DestinationLat = requestDto.DestinationLat;
        order.DestinationLng = requestDto.DestinationLng;
        order.DestinationAddress = requestDto.DestinationAddress;
        order.DestinationPostalCode = requestDto.DestinationPostalCode;
        order.DestinationPlaque = requestDto.DestinationPlaque;
        order.DestinationUnit = requestDto.DestinationUnit;
        order.Description = requestDto.Description;
        OrderDraftWorkflow.ClearSelection(order);
        CarrierOrderStatus.Record(appDbContext, order, DateTimeOffset.UtcNow);
        await appDbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new() { Succuss = true, Data = new() { OrderId = order.Id, OrderNumber = order.OrderNumber } };
    }
}
