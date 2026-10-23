using Logiwall.Shared.Dtos.Providers.Sep724;

namespace Logiwall.Application.Abstractions.Providers;
public interface ISep724Provider
{
    Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto);

    Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto);
}
