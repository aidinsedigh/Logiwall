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

    // Plan 014: type-safe, allow-listed filters and a page-size cap.
    private record Place(int Id, short City, short State, int? Maybe, string Name);

    private static IQueryable<Place> Places() =>
        Enumerable.Range(1, 150).Select(i => new Place(i, (short)(i % 3), (short)(i % 2), i % 2 == 0 ? i : null, $"Place {i}")).ToList().AsQueryable();

    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["city"] = nameof(Place.City),
        ["status"] = nameof(Place.State),
        ["maybe"] = nameof(Place.Maybe),
    };

    private static List<FilterGroup> One(string property, object? value, ConditionOperator op = ConditionOperator.Equal) =>
        [new() { Conditions = [new FilterCondition { PropertyName = property, Operator = op, Value = value }] }];

    [Fact]
    public void Equal_on_a_short_property_with_an_int_value_matches()
    {
        var result = Places().ApplyFilters(One("city", 2), Allowed).ToList();

        Assert.Equal(50, result.Count);
        Assert.All(result, p => Assert.Equal(2, p.City));
    }

    [Fact]
    public void Unknown_property_is_ignored_with_an_allow_list()
    {
        var result = Places().ApplyFilters(One("Name", "Place 1"), Allowed).ToList();

        Assert.Equal(150, result.Count);
    }

    [Fact]
    public void Alias_maps_status_to_State()
    {
        var result = Places().ApplyFilters(One("status", 0), Allowed).ToList();

        Assert.Equal(75, result.Count);
        Assert.All(result, p => Assert.Equal(0, p.State));
    }

    [Fact]
    public void Contains_on_a_non_string_property_is_ignored()
    {
        var result = Places().ApplyFilters(One("city", 1, ConditionOperator.Contains), Allowed).ToList();

        Assert.Equal(150, result.Count);
    }

    [Fact]
    public void Nullable_property_converts_the_value()
    {
        var result = Places().ApplyFilters(One("maybe", 4), Allowed).Select(p => p.Id).ToList();

        Assert.Equal(new[] { 4 }, result);
    }

    [Fact]
    public void Page_size_is_capped_at_100()
    {
        var result = Places().ApplyQueryParams(new GetListQueryParams { HasPagination = true, PageNumber = 1, PageSize = 1000 }).ToList();

        Assert.Equal(QueryableExtensions.MaxPageSize, result.Count);
    }
}
