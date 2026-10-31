namespace Logiwall.Shared.Dtos.OrderItems;

/// <param name="TotalCount">All orders that match the filters, before paging.</param>
/// <param name="Result">The requested page.</param>
public record OrderListResponseDto(int TotalCount, IList<OrderDto> Result);
