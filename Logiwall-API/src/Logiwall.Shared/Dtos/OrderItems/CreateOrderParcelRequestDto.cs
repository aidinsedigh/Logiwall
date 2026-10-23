using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.OrderItems;
public class CreateOrderParcelRequestDto
{
    public ParcelType ParcelTypeId { get; set; }

    public short PackageTypeId { get; set; }

    public int ParcelValue { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal Weight { get; set; }

    public int COD { get; set; }
}
