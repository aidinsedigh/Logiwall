namespace Logiwall.Shared.Dtos.OrderItems;

/// <summary>One provider's quotes for an order, and the pickup times the customer can choose.</summary>
public record OrderProviderOptionsDto(short ProviderId, string? ProviderName, IList<OrderInqueryOptionsDto> OrderOptions, IList<PickupDateDto> PickupDates);
