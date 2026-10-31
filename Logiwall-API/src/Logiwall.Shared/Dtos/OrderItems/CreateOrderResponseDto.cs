namespace Logiwall.Shared.Dtos.OrderItems;
public record CreateOrderResponseDto
{
    public string? OrderNumber { get; set; }

    public int OrderId { get; set; }

}
