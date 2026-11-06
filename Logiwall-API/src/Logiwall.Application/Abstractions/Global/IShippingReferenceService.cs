using Logiwall.Shared.Dtos.OrderItems;

namespace Logiwall.Application.Abstractions.Global;

public interface IShippingReferenceService
{
    Task<IReadOnlyList<ShippingReferenceResponseDto>> GetAsync(string providerCode, string category, CancellationToken cancellationToken,
        ShippingReferenceFilterDto? filter = null);
    Task SynchronizeAsync(CancellationToken cancellationToken);
}
