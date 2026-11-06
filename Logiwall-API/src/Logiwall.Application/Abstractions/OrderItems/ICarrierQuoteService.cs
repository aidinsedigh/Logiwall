using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.OrderItems;
namespace Logiwall.Application.Abstractions.OrderItems;
public interface ICarrierQuoteService
{
    Task<ServiceResponse<IList<OrderProviderOptionsDto>>> GetAsync(int orderId, int userId, CancellationToken cancellationToken,
        Logiwall.Shared.Enums.Constants.PaymentMethod paymentMethod = Logiwall.Shared.Enums.Constants.PaymentMethod.CashOnDelivery,
        int? serviceReferenceId = null);
}
