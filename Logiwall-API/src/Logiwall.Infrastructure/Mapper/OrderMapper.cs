using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Shared.Dtos.OrderItems;
using Riok.Mapperly.Abstractions;

namespace Logiwall.Infrastructure.Mapper;

[Mapper(UseDeepCloning = true)]
public static partial class OrderMapper
{
    // OrderService.GetOrderListAsync fills the names, which come from other tables.
    [MapProperty(nameof(Order.State), nameof(OrderDto.Status))]
    [MapperIgnoreTarget(nameof(OrderDto.StatusName))]
    [MapperIgnoreTarget(nameof(OrderDto.OriginCityName))]
    [MapperIgnoreTarget(nameof(OrderDto.DestinationCityName))]
    public static partial OrderDto Map(this Order source);

    public static partial Order Map(this CreateOrderRequestDto source);
    //public static Order Map(this CreateOrderRequestDto source)
    //{
    //    var destination = source.MapInternal();
    //    destination.OrderParcels = source.OrderParcels.Select(t => t.MapParcels()).ToList();
    //    return destination;
    //}

    public static partial OrderParcel MapParcels(this CreateOrderParcelRequestDto source);
}
