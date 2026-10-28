using System.Linq.Expressions;
using System.Reflection;
using Logiwall.Shared.Dtos.Global;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Extensions;
public static class QueryableExtensions
{
    /// <summary>The largest page a client can request.</summary>
    public const int MaxPageSize = 100;

    private static readonly MethodInfo OrderByMethod = QueryableMethod(nameof(Queryable.OrderBy));
    private static readonly MethodInfo OrderByDescendingMethod = QueryableMethod(nameof(Queryable.OrderByDescending));
    private static readonly MethodInfo ThenByMethod = QueryableMethod(nameof(Queryable.ThenBy));
    private static readonly MethodInfo ThenByDescendingMethod = QueryableMethod(nameof(Queryable.ThenByDescending));

    /// <param name="allowedProperties">
    /// Maps a case-insensitive client property name to the entity property name. When it is set,
    /// conditions on any other name are ignored.
    /// </param>
    public static IQueryable<T> ApplyQueryParams<T>(this IQueryable<T> query, GetListQueryParams queryParams,
        IReadOnlyDictionary<string, string>? allowedProperties = null)
    {
        query = query.ApplyFilters(queryParams.FilterGroups, allowedProperties);

        query = query.ApplySorting(queryParams.SortConditions, allowedProperties);

        if (queryParams.HasPagination)
        {
            query = query.ApplyPagination(queryParams.PageNumber, queryParams.PageSize);
        }

        return query;
    }

    /// <summary>
    /// Filters by the client's conditions. A condition is skipped when its property is not allowed,
    /// its value does not convert to the property type, or its operator does not apply to that type.
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<FilterGroup> filterGroups,
        IReadOnlyDictionary<string, string>? allowedProperties = null)
    {
        if (filterGroups == null || !filterGroups.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;
        foreach (var group in filterGroups)
        {
            Expression? groupExpression = null;

            foreach (var condition in group.Conditions)
            {
                var comparison = BuildComparison(parameter, condition, allowedProperties);
                if (comparison is null)
                    continue;

                if (groupExpression == null)
                {
                    groupExpression = comparison;
                }
                else
                {
                    groupExpression = group.LogicalOperator == LogicalOperator.And
                        ? Expression.AndAlso(groupExpression, comparison)
                        : Expression.OrElse(groupExpression, comparison);
                }
            }

            if (groupExpression is null)
                continue;

            // Combine groups using AND between them
            combinedExpression = combinedExpression == null ? groupExpression : Expression.AndAlso(combinedExpression, groupExpression);
        }

        if (combinedExpression is null)
            return query;

        var predicate = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(predicate);
    }

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, List<SortCondition> sortConditions,
        IReadOnlyDictionary<string, string>? allowedProperties = null)
    {
        if (sortConditions == null || !sortConditions.Any())
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var condition in sortConditions)
        {
            var name = ResolvePropertyName(condition.PropertyName, allowedProperties);
            if (name is null)
                continue;

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, name);
            var selector = Expression.Lambda(property, parameter);

            var method = orderedQuery is null
                ? (condition.Ascending ? OrderByMethod : OrderByDescendingMethod)
                : (condition.Ascending ? ThenByMethod : ThenByDescendingMethod);

            orderedQuery = (IOrderedQueryable<T>)method.MakeGenericMethod(typeof(T), property.Type)
                .Invoke(null, new object[] { orderedQuery ?? query, selector })!;
        }

        return orderedQuery ?? query;
    }

    /// <summary>Pages the query. <paramref name="pageSize"/> is clamped to 1..<see cref="MaxPageSize"/>, and the first page is 1.</summary>
    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        pageNumber = Math.Max(pageNumber, 1);
        return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    private static string? ResolvePropertyName(string? clientName, IReadOnlyDictionary<string, string>? allowedProperties)
    {
        if (string.IsNullOrEmpty(clientName))
            return null;

        if (allowedProperties is null)
            return clientName;

        return allowedProperties.TryGetValue(clientName, out var name) ? name : null;
    }

    private static Expression? BuildComparison(ParameterExpression parameter, FilterCondition condition, IReadOnlyDictionary<string, string>? allowedProperties)
    {
        var name = ResolvePropertyName(condition.PropertyName, allowedProperties);
        if (name is null)
            return null;

        var member = Expression.Property(parameter, name);
        if (TryConvert(condition.Value, member.Type, out var converted) is false)
            return null;

        var value = Expression.Constant(converted, member.Type);
        return condition.Operator switch
        {
            ConditionOperator.Equal => Expression.Equal(member, value),
            ConditionOperator.NotEqual => Expression.NotEqual(member, value),
            ConditionOperator.GreaterThan => Expression.GreaterThan(member, value),
            ConditionOperator.LessThan => Expression.LessThan(member, value),
            ConditionOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, value),
            ConditionOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, value),
            ConditionOperator.Contains when member.Type == typeof(string) => Expression.Call(member, nameof(string.Contains), null, value),
            _ => null,
        };
    }

    /// <summary>Converts a deserialized filter value (int, long, double, bool or string) to the property type.</summary>
    private static bool TryConvert(object? value, Type memberType, out object? converted)
    {
        converted = null;
        if (value is null)
            return true;

        var targetType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        try
        {
            if (targetType == typeof(DateTimeOffset))
                converted = DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
            else if (targetType.IsEnum)
                converted = Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
            else
                converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private static MethodInfo QueryableMethod(string name) =>
        typeof(Queryable).GetMethods().Single(m => m.Name == name && m.GetParameters().Length == 2);
}
