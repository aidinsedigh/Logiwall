namespace Logiwall.Shared.Dtos.OrderItems;
public record OrderInqueryOptionsDto
{
    public int Id { get; set; }

    public string? DelivereDate { get; set; }

    public decimal BaseAmount { get; set; }

    public decimal PackageAmount { get; set; }

    /// <summary>No source yet: no provider quotes a pickup price.</summary>
    public decimal PickupAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }
}
