using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Infrastructure.Providers.PostalServices;
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

    public async Task<ServiceResponse<PaymentResponseDto>> PaymentOrderStatusAsync(PaymentRequestDto requestDto, int userId)
    {
        try
        {
            var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId);
            if (order == null)
            {
                return new ServiceResponse<PaymentResponseDto>
                {
                    Data = new PaymentResponseDto
                    {
                        OrderId = requestDto.OrderId,
                        Status = PaymentStatus.UnSuccess,
                        Message = AppStrings.InvalidOrder
                    },
                    Succuss = false,
                    Message = AppStrings.InvalidOrder
                };
            }

            //todo Send Payment Request to Gateway
            var result = new PaymentResponseDto
            {
                OrderId = requestDto.OrderId,
                Status = PaymentStatus.Success,
                Message = string.Empty,
            };

            if (result.Status == PaymentStatus.UnSuccess)
            {
                return new ServiceResponse<PaymentResponseDto>
                {
                    Data = result,
                    Succuss = false,
                    Message = result.Message
                };
            }
            //todo Change order state
            var payment = await appDbContext.OrderPayments.FirstOrDefaultAsync(t => t.PayTrackingCode == requestDto.PayTrackingCode && t.OrderId == order.Id);

            if (payment is not null)
            {
                payment.Status = result.Status;
                appDbContext.Update(payment);
            }
            else
            {
                payment = requestDto.Map();
                payment.Status = result.Status;

                await appDbContext.AddAsync(payment);
            }

            await appDbContext.SaveChangesAsync();

            return new ServiceResponse<PaymentResponseDto>
            {
                Data = result,
                Succuss = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PaymentOrderStatusAsync failed");
            return new ServiceResponse<PaymentResponseDto>
            {
                Data = new PaymentResponseDto
                {
                    OrderId = requestDto.OrderId,
                    Status = PaymentStatus.UnSuccess,
                    Message = "An unexpected error occurred."
                },
                Succuss = false,
                Message = "An unexpected error occurred."
            };
        }
    }
}
