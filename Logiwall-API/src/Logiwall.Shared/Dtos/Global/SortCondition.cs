namespace Logiwall.Shared.Dtos.Global;
public record SortCondition
{
    public string PropertyName { get; set; }
    public bool Ascending { get; set; } = true;
}
