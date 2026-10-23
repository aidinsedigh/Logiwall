using Logiwall.Domain.Entities.OrderItems;

namespace Logiwall.Domain.Entities.People;
public class Customer
{
    public int Id { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    public int PersonId { get; set; }

    public Person Person { get; set; } = default!;

    public IList<CustomerContact> CustomerContacts { get; set; } = [];

    public IList<Order> Orders { get; set; } = [];
}
