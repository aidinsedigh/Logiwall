using Logiwall.Shared.Dtos.Global;
using Logiwall.Shared.Extensions;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.UnitTests.Extensions;

public class QueryableExtensionsTests
{
    private record Item(int Id, string Name);

    private static IQueryable<Item> Items() =>
        Enumerable.Range(1, 5).Select(i => new Item(i, $"Item {i}")).ToList().AsQueryable();

    [Fact]
    public void ApplyPagination_returns_the_requested_page()
    {
        var result = Items().ApplyPagination(pageNumber: 2, pageSize: 2).Select(t => t.Id).ToList();

        Assert.Equal(new[] { 3, 4 }, result);
    }

    [Fact]
    public void ApplyFilters_with_one_equal_condition_returns_the_match()
    {
        var groups = new List<FilterGroup>
        {
            new() { Conditions = [new FilterCondition { PropertyName = nameof(Item.Id), Operator = ConditionOperator.Equal, Value = 3 }] }
        };

        var result = Items().ApplyFilters(groups).Select(t => t.Id).ToList();

        Assert.Equal(new[] { 3 }, result);
    }

    [Fact]
    public void ApplySorting_descending_reverses_the_order()
    {
        var sort = new List<SortCondition> { new() { PropertyName = nameof(Item.Id), Ascending = false } };

        var result = Items().ApplySorting(sort).Select(t => t.Id).ToList();

        Assert.Equal(new[] { 5, 4, 3, 2, 1 }, result);
    }

    [Fact]
    public void ApplyQueryParams_without_pagination_returns_every_item()
    {
        var result = Items().ApplyQueryParams(new GetListQueryParams { HasPagination = false }).ToList();

        Assert.Equal(5, result.Count);
    }
}
