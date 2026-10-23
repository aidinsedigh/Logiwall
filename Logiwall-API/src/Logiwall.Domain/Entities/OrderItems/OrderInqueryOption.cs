namespace Logiwall.Domain.Entities.OrderItems;
public class OrderInqueryOption
{
    public int Id { get; set; }

    public short ServiceType { get; set; }

    public short ServiceSLA { get; set; }

    public decimal PackagingAmount { get; set; }

    public decimal ServiceAmount { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public decimal Tax { get; set; }

    [Required, StringLength(50)]
    public string DelivereDate { get; set; } = string.Empty;

    public bool Choosen { get; set; }

    public int OrderId { get; set; }

    public short ServiceProviderId { get; set; }

    public Order Order { get; set; } = default!;

    public ServiceProvider ServiceProvider { get; set; } = default!;
}
