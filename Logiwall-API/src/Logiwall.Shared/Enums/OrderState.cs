using System.ComponentModel;

namespace Logiwall.Shared.Enums;
public enum OrderState: short
{
    [Description("ثبت شده")]
    Created = 0,
    [Description("انتخاب سرویس پستی")]
    SelectedServiceProvider = 1,
    [Description("پرداخت شده")]
    Paid = 2,
}
