using System.ComponentModel;

namespace Logiwall.Shared.Enums;
public enum PaymentStatus
{
    [Description("ناموفق")]
    UnSuccess = 0,
    [Description("موفق")]
    Success = 1,
    [Description("در انتظار")]
    Pending = 2,
}
