namespace Logiwall.Shared.Dtos.OrderItems;
public class SelectOptionRequestDto
{
    public int OrderId { get; set; }

    public int OrderInqueryOptionId { get; set; }

    [Required]
    public string? Channel { get; set; }

    [Required]
    public string? CallbackUrl { get; set; }
}
