namespace Logiwall.Shared.Dtos.Dashboard;

public record ProductPercentagePerCategoryResponseDto
{
    public string? CategoryName { get; set; }

    public string? CategoryColor { get; set; }

    public float ProductPercentage { get; set; }
}
