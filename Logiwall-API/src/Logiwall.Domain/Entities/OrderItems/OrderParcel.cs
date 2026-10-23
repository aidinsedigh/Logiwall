using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Domain.Entities.OrderItems;
public class OrderParcel
{
    public int Id { get; set; }

    public ParcelType ParcelTypeId { get; set; }

    public short PackageTypeId { get; set; }

    public int ParcelValue { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal Weight { get; set; }

    public int COD { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = default!;
}
