using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.OrderItems;
public record SelectPaymentTypeRequestDto
{
    public PaymentMethod PaymentType { get; set; }

    public int OrderId { get; set; }

    /// <summary>A <see cref="PickupOptionDto.Id"/> from <c>SelectOption</c>.</summary>
    public int PickupOptionId { get; set; }

    /// <summary>The front sends null when it has no channel; the API then uses <c>pwa</c>.</summary>
    public string? Channel { get; set; }

    [Required]
    public string? CallbackUrl { get; set; }
}
