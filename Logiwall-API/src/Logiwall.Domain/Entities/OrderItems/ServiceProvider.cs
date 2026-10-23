using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Domain.Entities.OrderItems;
public class ServiceProvider
{
    public short Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Title { get; set; } = default!;
}
