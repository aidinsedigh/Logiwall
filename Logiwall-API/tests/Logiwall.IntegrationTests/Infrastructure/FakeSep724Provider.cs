using System.Security.Cryptography;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers.Sep724;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>A payment gateway that always issues a link and records what it was asked to charge.</summary>
public class FakeSep724Provider : ISep724Provider
{
    private int tokenCalls;

    public int TokenCalls => tokenCalls;

    public decimal? LastAmount { get; private set; }

    public Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto)
    {
        Interlocked.Increment(ref tokenCalls);
        LastAmount = requestDto.Amount;

        return Task.FromResult(new SepResponseBase<GetPaymentTokenResponseDto>
        {
            Data = new GetPaymentTokenResponseDto
            {
                RequestId = RandomNumberGenerator.GetInt32(1, int.MaxValue),
                ReferenceCode = Guid.NewGuid(),
                PaymentLink = "https://pay.test/x"
            }
        });
    }

    public Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto)
        => throw new NotSupportedException("Phase B of plan 012 is not implemented.");
}
