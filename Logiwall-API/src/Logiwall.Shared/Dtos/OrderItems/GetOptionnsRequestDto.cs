namespace Logiwall.Shared.Dtos.OrderItems;
public record GetOptionnsRequestDto
{
    public int OrderId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public int? ServiceReferenceId { get; set; }
    [EnumDataType(typeof(Constants.PaymentMethod), ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public Constants.PaymentMethod PaymentType { get; set; } = Constants.PaymentMethod.CashOnDelivery;
}
