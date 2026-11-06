using Logiwall.Shared.Enums;

namespace Logiwall.UnitTests.Services;

public class OrderStatusAggregationTests
{
    [Fact]
    public void Delivery_requires_all_parcels_and_unknown_codes_do_not_guess_a_status()
    {
        Assert.Equal(OrderStatusCode.Delivered, OrderStatusAggregation.FromParcels([OrderStatusCode.Delivered, OrderStatusCode.Delivered]));
        Assert.Equal(OrderStatusCode.Processing, OrderStatusAggregation.FromParcels([OrderStatusCode.Delivered, OrderStatusCode.Processing]));
        Assert.Equal(OrderStatusCode.Registered, OrderStatusAggregation.FromParcels([OrderStatusCode.Delivered, null]));
        Assert.Equal(OrderStatusCode.Processing, OrderStatusAggregation.FromParcels([OrderStatusCode.Cancelled, OrderStatusCode.Processing]));
        Assert.Equal(OrderStatusCode.AwaitingPickup, OrderStatusAggregation.FromParcels([OrderStatusCode.Processing, OrderStatusCode.AwaitingPickup]));
        Assert.Equal(OrderStatusCode.OriginTransit, OrderStatusAggregation.FromParcels([OrderStatusCode.Collected, OrderStatusCode.OriginTransit]));
    }
}
