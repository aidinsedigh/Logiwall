using Logiwall.Application.Abstractions.OrderItems;
using Logiwall.Infrastructure.Mapper;
using Logiwall.Infrastructure.Providers.PostalServices;
using Logiwall.Server.Data;
using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Dtos.PaymentItems;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.Infrastructure.Services.OrderItems;
public class PaymentService : IPaymentService
{
    private readonly AppDbContext appDbContext;
    private readonly Func<ProviderType, PostalServiceBase> postalServiceFactory;

    public PaymentService(AppDbContext appDbContext,
        Func<ProviderType, PostalServiceBase> postalServiceFactory)
    {
        this.appDbContext = appDbContext;
        this.postalServiceFactory = postalServiceFactory;
    }

    public async Task<ServiceResponse<PaymentResponseDto>> PaymentOrderStatusAsync(PaymentRequestDto requestDto, int userId)
    {
        try
        {
            var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId);
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
            var payment = await appDbContext.OrderPayments.FirstOrDefaultAsync(t => t.PayTrackingCode == requestDto.PayTrackingCode);

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
            return new ServiceResponse<PaymentResponseDto>
            {
                Data = new PaymentResponseDto
                {
                    OrderId = requestDto.OrderId,
                    Status = PaymentStatus.UnSuccess,
                    Message = ex.Message
                },
                Succuss = false,
                Message = ex.Message
            };
        }
    }
}
