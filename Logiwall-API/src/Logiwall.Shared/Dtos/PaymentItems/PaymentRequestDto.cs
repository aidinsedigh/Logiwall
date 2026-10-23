using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.PaymentItems;
public class PaymentRequestDto
{
    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public PaymentType PayType { get; set; }

    public int? MerchantId { get; set; }

    public int? TerminalId { get; set; }

    [Required]
    public string PayTrackingCode { get; set; }

    [Required]
    public DateTimeOffset PayDate { get; set; }

}
