namespace Logiwall.Shared.Dtos.OrderItems;
public record PickupDateDto(string Id, string Description, IList<PickupTimeDto> Times);
