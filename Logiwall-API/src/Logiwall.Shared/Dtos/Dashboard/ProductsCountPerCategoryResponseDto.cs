namespace Logiwall.Shared.Dtos.Dashboard;

public record ProductsCountPerCategoryResponseDto
{
    public string? CategoryName { get; set; }

    public string? CategoryColor { get; set; }

    public int ProductCount { get; set; }
}
