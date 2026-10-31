namespace Logiwall.Shared.Dtos.OrderItems;
public record SelectOptionRequestDto
{
    public int OrderId { get; set; }

    public int OrderInqueryOptionId { get; set; }

    /// <summary>A <see cref="PickupTimeDto.Id"/> from <c>GetOptions</c>.</summary>
    [Required]
    public string? PickupId { get; set; }
}
