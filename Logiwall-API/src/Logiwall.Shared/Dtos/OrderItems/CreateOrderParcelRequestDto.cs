using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.OrderItems;
public record CreateOrderParcelRequestDto
{
    [EnumDataType(typeof(ParcelType), ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public ParcelType ParcelTypeId { get; set; }

    /// <summary>0: no carrier packaging (reference may be null); 1: selected carrier packaging is required.</summary>
    [Range(0, 1, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public short PackageTypeId { get; set; }

    public int ParcelValue { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal Weight { get; set; }

    public int COD { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public int? PackagingReferenceId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = nameof(AppStrings.ModelBindingValueIsInvalid))]
    public int? ContentReferenceId { get; set; }
}
