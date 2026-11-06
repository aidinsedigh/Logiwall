using System.ComponentModel;

namespace Logiwall.Shared.Enums;

public enum OrderPaymentState
{
    [Description("پرداخت نشده")] Unpaid,
    [Description("پرداخت شده")] Paid,
    [Description("پرداخت ناموفق")] Failed,
    [Description("بخشی از مبلغ پرداخت شده")] PartiallyPaid
}
