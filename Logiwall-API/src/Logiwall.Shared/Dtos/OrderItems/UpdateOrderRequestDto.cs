namespace Logiwall.Shared.Dtos.OrderItems;

public record UpdateOrderRequestDto : CreateOrderRequestDto
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }
}
