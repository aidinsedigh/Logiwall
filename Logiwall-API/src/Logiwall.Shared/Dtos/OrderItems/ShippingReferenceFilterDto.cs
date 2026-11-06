namespace Logiwall.Shared.Dtos.OrderItems;

public record ShippingReferenceFilterDto
{
    [Range(0, 1, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))] public short? PackageTypeId { get; init; }
    [Range(1, int.MaxValue, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))] public int? PackagingReferenceId { get; init; }
    [Range(1, int.MaxValue, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))] public int? ContentReferenceId { get; init; }
}
