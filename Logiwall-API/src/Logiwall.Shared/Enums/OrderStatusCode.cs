using System.ComponentModel;

namespace Logiwall.Shared.Enums;

public enum OrderStatusCode : short
{
    [Description("تکمیل نشده")] Draft = 0,
    [Description("ثبت شده")] Registered = 10,
    [Description("در انتظار جمع‌آوری")] AwaitingPickup = 12,
    [Description("جمع‌آوری‌شده")] Collected = 13,
    [Description("در مسیر مبدا")] OriginTransit = 14,
    [Description("در حال پردازش")] Processing = 15,
    [Description("رسیده به شهر مقصد")] AtDestination = 16,
    [Description("در حال برگشت به مرکز قبلی")] ReturningToPreviousNode = 17,
    [Description("در حال عودت به فرستنده")] ReturningToSender = 18,
    [Description("عودت تحویل فرستنده شد")] ReturnedToSender = 19,
    [Description("تحویل موفق به گیرنده")] Delivered = 20,
    [Description("لغوشده")] Cancelled = 21,
    [Description("باطل‌شده")] Invalidated = 22,
}

public static class OrderStatusAggregation
{
    public static OrderStatusCode FromParcels(IEnumerable<OrderStatusCode?> parcelStatuses,
        OrderStatusCode lastKnown = OrderStatusCode.Registered)
    {
        var statuses = parcelStatuses.ToArray();
        if (statuses.Length == 0 || statuses.Any(s => s is null)) return lastKnown;
        var distinct = statuses.Select(s => s!.Value).Distinct().ToArray();
        if (distinct.Length == 1) return distinct[0];
        // Delivery is complete only when every parcel is delivered. Otherwise show the outstanding stage.
        OrderStatusCode[] progress = [OrderStatusCode.Draft, OrderStatusCode.Registered,
            OrderStatusCode.AwaitingPickup, OrderStatusCode.OriginTransit, OrderStatusCode.Collected,
            OrderStatusCode.Processing, OrderStatusCode.AtDestination, OrderStatusCode.ReturningToPreviousNode,
            OrderStatusCode.ReturningToSender, OrderStatusCode.ReturnedToSender, OrderStatusCode.Cancelled,
            OrderStatusCode.Invalidated, OrderStatusCode.Delivered];
        return distinct.OrderBy(s => Array.IndexOf(progress, s)).First();
    }
}
