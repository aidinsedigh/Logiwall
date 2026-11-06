namespace Logiwall.Shared.Dtos.OrderItems;

/// <summary>The selected provider's price options for an order.</summary>
public record OrderProviderOptionsDto(short ProviderId, string? ProviderName, IList<OrderInqueryOptionsDto> OrderOptions);
