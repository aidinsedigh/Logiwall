using System.Linq.Expressions;
using Logiwall.Shared.Dtos.Global;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Extensions;
public static class QueryableExtensions
{
    public static IQueryable<T> ApplyQueryParams<T>(this IQueryable<T> query, GetListQueryParams queryParams)
    {
        query = query.ApplyFilters(queryParams.FilterGroups);

        query = query.ApplySorting(queryParams.SortConditions);

        if (queryParams.HasPagination)
        {
            query = query.ApplyPagination(queryParams.PageNumber, queryParams.PageSize);
        }

        return query;
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<FilterGroup> filterGroups)
    {
        if (filterGroups == null || !filterGroups.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression combinedExpression = null;
        foreach (var group in filterGroups)
        {
            Expression groupExpression = null;

            foreach (var condition in group.Conditions)
            {
                var property = Expression.Property(parameter, condition.PropertyName);
                Expression comparison = null;

                switch (condition.Operator)
                {
                    case ConditionOperator.Equal:
                        comparison = Expression.Equal(property, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.GreaterThan:
                        comparison = Expression.GreaterThan(property, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.LessThan:
                        comparison = Expression.LessThan(property, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.Contains:
                        comparison = Expression.Call(property, "Contains", null, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.NotEqual:
                        comparison = Expression.NotEqual(property, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.GreaterThanOrEqual:
                        comparison = Expression.GreaterThanOrEqual(property, Expression.Constant(condition.Value));
                        break;
                    case ConditionOperator.LessThanOrEqual:
                        comparison = Expression.LessThanOrEqual(property, Expression.Constant(condition.Value));
                        break;
                }

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

            if (combinedExpression == null)
            {
                combinedExpression = groupExpression;
            }
            else
            {
                // Combine groups using AND/OR between them
                combinedExpression = Expression.AndAlso(combinedExpression, groupExpression);
            }
        }
        var predicate = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(predicate);
    }

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, List<SortCondition> sortConditions)
    {
        if (sortConditions == null || !sortConditions.Any())
            return query;

        IOrderedQueryable<T> orderedQuery = null;

        for (int i = 0; i < sortConditions.Count; i++)
        {
            var condition = sortConditions[i];
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, condition.PropertyName);
            var selector = Expression.Lambda(property, parameter);

            if (i == 0) // First sort condition (OrderBy / OrderByDescending)
            {
                string methodName = condition.Ascending ? "OrderBy" : "OrderByDescending";
                var method = typeof(Queryable).GetMethods()
                                              .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                                              .Single()
                                              .MakeGenericMethod(typeof(T), property.Type);

                orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { query, selector });
            }
            else // Subsequent sort conditions (ThenBy / ThenByDescending)
            {
                string methodName = condition.Ascending ? "ThenBy" : "ThenByDescending";
                var method = typeof(Queryable).GetMethods()
                                              .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                                              .Single()
                                              .MakeGenericMethod(typeof(T), property.Type);

                orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { orderedQuery, selector });
            }
        }

        return orderedQuery ?? query;
    }

    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}


