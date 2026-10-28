namespace Logiwall.Shared.Dtos.Global;
public record GetListQueryParams
{
    public bool HasPagination { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public List<FilterGroup> FilterGroups { get; set; } = new List<FilterGroup>();
    public List<SortCondition> SortConditions { get; set; } = new List<SortCondition>();
}
