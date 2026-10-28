namespace Logiwall.Shared.Dtos.OrderItems;

/// <param name="Id">The chosen <c>OrderInqueryOption.Id</c>; <c>SelectPaymentType</c> takes it as <c>pickupOptionId</c>.</param>
public record PickupOptionDto(int Id, string? ProviderPickupOptionId, decimal FinalAmount, decimal BasePrice, decimal ItemPrice, decimal Tax, string Description);
