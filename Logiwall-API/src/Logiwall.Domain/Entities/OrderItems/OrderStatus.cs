using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Domain.Entities.OrderItems;
public class OrderStatus: ICreationDateAware
{
    public int Id { get; set; }

    public int Status { get; set; }

    public int ServiceProviderId { get; set; }

    public short CityId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("EventDate")]
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.Now;

    public int OrderId { get; set; }

    public Order Order { get; set; }
}
