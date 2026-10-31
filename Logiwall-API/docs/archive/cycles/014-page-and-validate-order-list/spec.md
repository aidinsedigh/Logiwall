# Cycle 014 — The order list is async, bounded, deterministically ordered, and its filters work for the fields the frontend sends: spec

## Contents

- L19 Summary
- L28 Why this matters
- L42 Repo facts you need
- L57 Current state
- L82 Scope
- L95 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 014 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P3
- **Effort**: S
- **Risk**: LOW–MED (keeps the response shape; caps unpaged lists)
- **Depends on**: docs/archive/cycles/005-establish-test-and-ci-baseline/
- **Category**: perf / bug
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

`POST api/Order/GetOrders` has four problems:
- It iterates an `IQueryable` with `foreach`, which is synchronous DB I/O inside an async method.
- It returns the customer's **entire** history when `hasPagination` is false.
- It accepts any `PageSize`.
- It pages without a guaranteed order, so pages can overlap or skip rows.

Its dynamic filters are also broken for the frontend. The React orders screen
sends `propertyName` values `id`, `originCity`, `destinationCity` and `status`,
with numeric values and operator `50` (Equal). `originCity`/`destinationCity` are
`short` properties, so `Expression.Equal(short, int)` throws. `status` matches no
property at all (the entity uses `State`). Both currently surface as HTTP 500s.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- The frontend calls (read-only reference):
  - `Logiwall-Front/src/features/Orders/api/orders.ts:23-28`: `{ hasPagination: true, pageNumber, pageSize: 3, filterGroups }`
  - `Logiwall-Front/src/features/home/components/index.tsx:24-28`: `{ hasPagination: false, pageNumber: 0, pageSize: 0 }`, i.e. "recent orders" on the home page
  - `Logiwall-Front/src/features/Orders/components/orders-filters.tsx:58`: builds `{ propertyName: key, value: Number(value), operator: 50 }` for keys `id`, `originCity`, `destinationCity`, `status`.
- **Keep the response shape**: `ServiceResponse<IList<OrderDto>>`. Do not switch to `PagedResult<T>`; the frontend parses the current shape.
- `FilterCondition.Value` is `object?`, deserialized by `src/Logiwall.Shared/Utils/ObjectToStringConverter.cs` into int, long, double, bool or string.
- `System.Linq.Expressions.Expression.Property(expr, name)` resolves names **case-insensitively**, so `id` already finds `Id`.
- `Order` properties relevant to filtering: `Id` (int), `OriginCity` (short), `DestinationCity` (short), `State` (short), `OrderNumber` (string?), `CreationDate` (DateTimeOffset).
- `OrderDto` is produced by the Mapperly mapper `src/Logiwall.Infrastructure/Mapper/OrderMapper.cs` (`public static partial OrderDto Map(this Order source);`).
- `QueryableExtensions` lives in **Shared**, which is also compiled for the browser (Blazor). Keep it free of EF Core references.
- Unit tests from plan 005: `tests/Logiwall.UnitTests/Extensions/QueryableExtensionsTests.cs` (4 characterization tests). They must keep passing, or be updated deliberately where this plan changes behavior.

## Current state

`src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`, `GetOrderListAsync`:
```csharp
var customer = await appDbContext.Customers.FirstOrDefaultAsync(t => t.Person.UserId == requestDto.UserId);
...
var orders = appDbContext.Orders.Where(t => t.CustomerId == customer.Id);
var query = orders.ApplyQueryParams(requestDto.QueryParams);
//ToDo add filter & sort
var result = new List<OrderDto>();
foreach (var order in query)
{
    result.Add(order.Map());
}
```

`src/Logiwall.Shared/Extensions/QueryableExtensions.cs`:
- `ApplyQueryParams` applies filters, then sorting, then pagination when `HasPagination` is true.
- `ApplyFilters` does `Expression.Property(parameter, condition.PropertyName)` and
  `Expression.Constant(condition.Value)` for every operator. The `switch` has no `default`, so an unknown operator leaves `comparison` null.
- `ApplySorting` uses reflection to look up `OrderBy`/`ThenBy` on every call.
- `ApplyPagination`: `query.Skip((pageNumber - 1) * pageSize).Take(pageSize)`.

`src/Logiwall.Shared/Dtos/Global/GetListQueryParams.cs`: `HasPagination` (false), `PageNumber` (1), `PageSize` (10), `FilterGroups`, `SortConditions`.

## Scope

**In scope**:
- `src/Logiwall.Shared/Extensions/QueryableExtensions.cs`
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` (`GetOrderListAsync` only)
- `tests/Logiwall.UnitTests/Extensions/QueryableExtensionsTests.cs`
- `tests/Logiwall.IntegrationTests/Orders/OrderListTests.cs` (create)

**Out of scope**:
- `GetListQueryParams` property names and defaults (wire contract).
- The response shape, and adding a total count.
- `Logiwall-Front/src/hooks/react-query/use-query-orders.ts`, which calls this POST endpoint with GET (already broken; mention it in your report).

## Maintenance notes

- Add new filterable fields to `OrderFilterableProperties`. Never pass a client property name through unfiltered.
- When the frontend is ready for a total count, introduce `PagedResult<OrderDto>` behind a new action rather than changing this one.
