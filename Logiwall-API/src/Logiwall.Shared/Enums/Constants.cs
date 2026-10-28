using System.ComponentModel;

namespace Logiwall.Shared.Enums;
public class Constants
{
    public enum PersonKind
    {
        [Description("حقیقی")]
        Private = 1,
        [Description("حقوقی")]
        Legal = 2
    }

    public enum Gender
    {
        [Description("مرد")]
        Male = 3,
        [Description("زن")]
        Female = 4,
    }

    public enum PaymentType
    {
        [Description("کیف پول")]
        Wallet = 5,
        [Description("پرداخت بانکی")]
        BankGateway = 6,
    }

    /// <summary>The front's payment choice on the pay step.</summary>
    public enum PaymentMethod
    {
        [Description("پرداخت آنلاین")]
        Online = 71,
        [Description("پس کرایه")]
        CashOnDelivery = 72,
    }

    public enum ParcelType : short
    {
        [Description("پاکت")]
        Envelope = 7,
        [Description("بسته")]
        Package = 8,
    }

    public enum StaticDataType : short
    {
        [Description("کشور")]
        Country = 9,
        [Description("استان")]
        Province = 10,
        [Description("شهر مبدا")]
        OriginCity = 11,
        [Description("شهر مقصد")]
        DestinationCity = 12,
    }

    public enum LoginProvider : short
    {
        Logiwall = 20,
        App724 = 21,
    }

    public enum ConditionOperator
    {
        Equal = 50,
        NotEqual = 51,
        GreaterThan = 52,
        GreaterThanOrEqual = 53,
        LessThan = 54,
        LessThanOrEqual = 55,
        Contains = 56,
    }

    public enum LogicalOperator
    {
        And = 60,
        Or = 61,
    }
}
