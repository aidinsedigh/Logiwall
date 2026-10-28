namespace Logiwall.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public record RefreshRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? RefreshToken { get; set; }
}