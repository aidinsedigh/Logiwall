using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Server.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.PaymentItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logiwall.Infrastructure.Services.OrderItems;
public class PaymentService : IPaymentService
{
    private readonly AppDbContext appDbContext;
    private readonly ILogger<PaymentService> logger;

    public PaymentService(AppDbContext appDbContext,
        ILogger<PaymentService> logger)
    {
        this.appDbContext = appDbContext;
        this.logger = logger;
    }

    public async Task<ServiceResponse<PaymentResponseDto>> VerifyPaymentAsync(VerifyPaymentRequestDto requestDto, int userId, CancellationToken cancellationToken)
    {
        try
        {
            var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId, cancellationToken);
            if (order == null)
            {
                return Failure(requestDto.OrderId, AppStrings.InvalidOrder);
            }

            var payment = await appDbContext.OrderPayments
                .Where(t => t.OrderId == order.Id && t.GatewayReferenceCode != null)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (payment is null)
            {
                return Failure(order.Id, "No payment to verify");
            }

            // Idempotent: a verified payment returns its stored outcome without calling the gateway again.
            if (payment.VerifiedAt is not null && payment.Status is PaymentStatus.Success or PaymentStatus.UnSuccess)
            {
                return Result(order.Id, payment.Status);
            }

            var verifiedStatus = await VerifyWithGatewayAsync(payment, cancellationToken);
            if (verifiedStatus is null)
            {
                return Failure(order.Id, "Payment verification is not available yet");
            }

            return Result(order.Id, verifiedStatus.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VerifyPaymentAsync failed");
            return Failure(requestDto.OrderId, "An unexpected error occurred.");
        }
    }

    /// <returns>The verified status, or null when the gateway cannot verify the payment.</returns>
    private Task<PaymentStatus?> VerifyWithGatewayAsync(OrderPayment payment, CancellationToken cancellationToken)
    {
        // Phase B: implement with Sep724Provider.GetPaymentInfoAsync. It is the only place that may set PaymentStatus.Success.
        _ = payment;
        _ = cancellationToken;
        return Task.FromResult<PaymentStatus?>(null);
    }

    private static ServiceResponse<PaymentResponseDto> Result(int orderId, PaymentStatus status) => new()
    {
        Data = new PaymentResponseDto { OrderId = orderId, Status = status, Message = string.Empty },
        Succuss = status == PaymentStatus.Success,
    };

    private static ServiceResponse<PaymentResponseDto> Failure(int orderId, string message) => new()
    {
        Data = new PaymentResponseDto { OrderId = orderId, Status = PaymentStatus.UnSuccess, Message = message },
        Succuss = false,
        Message = message
    };
}
