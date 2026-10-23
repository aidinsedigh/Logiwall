using Logiwall.Domain.Entities.Identity;
using Logiwall.Domain.Entities.OrderItems;
using Logiwall.Shared.Dtos.Identity;
using Logiwall.Shared.Dtos.PaymentItems;
using Riok.Mapperly.Abstractions;

namespace Logiwall.Infrastructure.Mapper;

[Mapper(UseDeepCloning = true)]
internal static partial class PaymentMapper
{
    public static partial PaymentRequestDto Map(this OrderPayment source);

    private static partial OrderPayment MapInternal(this PaymentRequestDto source);

    public static OrderPayment Map(this PaymentRequestDto source)
    {
        var destination = source.MapInternal();
        destination.PaymentType = source.PayType;
        return destination;
    }
}
