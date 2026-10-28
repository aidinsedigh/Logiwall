using System.Security.Cryptography;
using Logiwall.Application.Abstractions.Providers;
using Logiwall.Shared.Dtos.Providers.Sep724;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>A payment gateway that issues a link (unless told to fail) and records what it was asked to charge.</summary>
public class FakeSep724Provider : ISep724Provider
{
    private int tokenCalls;

    public int TokenCalls => tokenCalls;

    public decimal? LastAmount { get; private set; }

    public string? LastChannel { get; private set; }

    /// <summary>When set, the gateway issues no link and returns this provider text as its title.</summary>
    public string? FailWithTitle { get; set; }

    /// <summary>When set, the next link carries this reference code instead of a new one.</summary>
    public Guid? NextReferenceCode { get; set; }

    public Task<SepResponseBase<GetPaymentTokenResponseDto>> GetPaymentTokenAsync(PaymentTokenRequestDto requestDto)
    {
        Interlocked.Increment(ref tokenCalls);
        LastAmount = requestDto.Amount;
        LastChannel = requestDto.Channel;

        if (FailWithTitle is not null)
            return Task.FromResult(new SepResponseBase<GetPaymentTokenResponseDto> { Title = FailWithTitle });

        return Task.FromResult(new SepResponseBase<GetPaymentTokenResponseDto>
        {
            Data = new GetPaymentTokenResponseDto
            {
                RequestId = RandomNumberGenerator.GetInt32(1, int.MaxValue),
                ReferenceCode = NextReferenceCode ?? Guid.NewGuid(),
                PaymentLink = "https://pay.test/x"
            }
        });
    }

    public Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto)
        => throw new NotSupportedException("Phase B of plan 012 is not implemented.");
}
