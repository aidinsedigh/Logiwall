# Plan 014: The order list is async, bounded, deterministically ordered, and its filters work for the fields the frontend sends

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Shared/Extensions/QueryableExtensions.cs Logiwall-API/src/Logiwall.Shared/Dtos/Global Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs Logiwall-API/src/Logiwall.Server/Controllers/Order/OrderController.cs`
> `OrderService.cs` will show changes from plans 002/009/011/012 in **other** methods. `GetOrderListAsync` itself should be unchanged. If it has changed, STOP.

## Status

- **Priority**: P3
- **Effort**: S
- **Risk**: LOW–MED (keeps the response shape; caps unpaged lists)
- **Depends on**: docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md
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

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` | all pass |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

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

## Git workflow

- Branch `advisor/014-order-list`. Commit per step. Do NOT push.

## Steps

### Step 1: Make filters type-safe, allow-listed and aliasable

Change `ApplyFilters` to take the allowed properties:
```csharp
public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<FilterGroup> filterGroups,
    IReadOnlyDictionary<string, string>? allowedProperties = null)
```
`allowedProperties` maps a **case-insensitive client name** to the **entity property name**.
When it is non-null:
- skip (ignore) any condition whose `PropertyName` is not a key;
- otherwise use the mapped name.

When it is null, keep today's behavior (name used as given). For each condition:
1. `var member = Expression.Property(parameter, name);`
2. Convert the value to the member type: unwrap `Nullable<>` to get `targetType`. If `condition.Value` is null,
   use `Expression.Constant(null, member.Type)`. Otherwise
   `Convert.ChangeType(condition.Value, targetType, CultureInfo.InvariantCulture)` for primitives,
   `DateTimeOffset.Parse(..., CultureInfo.InvariantCulture)` for `DateTimeOffset`, and
   `Enum.ToObject` for enums. Then `Expression.Constant(converted, member.Type)` (this works for nullable member types).
   If the conversion throws `FormatException`, `InvalidCastException` or `OverflowException`, skip the condition.
3. `Contains` only applies when `member.Type == typeof(string)`; skip it otherwise.
4. Add `default:` → skip the condition (don't leave `comparison` null).
5. If a group ends up with no conditions, skip the group. If no groups remain, return `query` unchanged.

`ApplySorting`: add the same optional `allowedProperties` parameter and semantics. Cache the four generic
method definitions (`OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`) in `static readonly MethodInfo` fields.

`ApplyQueryParams`: add an optional `allowedProperties` parameter and pass it through. Change pagination to:
```csharp
var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);
var pageNumber = Math.Max(queryParams.PageNumber, 1);
```

**Verify**: build → exit 0; `dotnet test tests/Logiwall.UnitTests` → the existing 4 still pass. If one fails because of the clamping, adjust that test's expectation, and say so in the test name or a comment.

### Step 2: Rewrite `GetOrderListAsync`

```csharp
private static readonly Dictionary<string, string> OrderFilterableProperties = new(StringComparer.OrdinalIgnoreCase)
{
    ["id"] = nameof(Order.Id),
    ["originCity"] = nameof(Order.OriginCity),
    ["destinationCity"] = nameof(Order.DestinationCity),
    ["status"] = nameof(Order.State),
    ["state"] = nameof(Order.State),
    ["orderNumber"] = nameof(Order.OrderNumber),
    ["creationDate"] = nameof(Order.CreationDate),
};
```
- Load only the customer ID: `await appDbContext.Customers.Where(t => t.Person.UserId == requestDto.UserId).Select(t => (int?)t.Id).FirstOrDefaultAsync()`. Keep the existing "There is no customer" branch when it is null.
- `var query = appDbContext.Orders.AsNoTracking().Where(t => t.CustomerId == customerId);`
- Apply filters and sorting with `OrderFilterableProperties`. **If no valid sort condition remains, apply `OrderByDescending(t => t.Id)`** before paging.
- Pagination: if `HasPagination`, use `ApplyPagination` with the clamped values from Step 1. Otherwise apply `.Take(100)` (the newest 100 orders, which is what the home page wants).
- Materialize with `await query.ToListAsync()`, then map with `.Select(o => o.Map()).ToList()`.
  (Mapperly maps in memory, so map *after* `ToListAsync`.)

**Verify**: build → exit 0; `grep -n "foreach (var order in query)" src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` → no output.

### Step 3: Tests

Unit tests (`QueryableExtensionsTests.cs`). Add a test record with `short City`, `int? Maybe`, `string Name`:
1. An Equal filter on the short property with an `int` value matches (no throw).
2. An unknown property with the allow-list → ignored (all rows returned).
3. A property alias (`status` → `State`) works.
4. `Contains` on a non-string property → ignored, no throw.
5. `PageSize = 1000` → at most 100 rows.

Integration tests (`Orders/OrderListTests.cs`). Insert 3 orders for one user via `AppDbContext` (copy the pattern from `OrderOwnershipTests`):
6. `hasPagination=true, pageNumber=1, pageSize=2` → 2 orders, the highest IDs first.
7. Filter `{ propertyName: "originCity", value: <one order's city>, operator: 50 }` → only that order. Previously this was a 500.
8. Filter `{ propertyName: "status", value: 0, operator: 50 }` → all 3 (state Created). Previously a 500.

**Verify**: both test projects pass.

## Test plan

See Step 3.

## Done criteria

- [ ] Build exits 0; unit tests (≥9) and integration tests pass
- [ ] Greps in Step 2 return no output
- [ ] Response shape unchanged: `ServiceResponse<IList<OrderDto>>` (check the controller and service signatures haven't changed)
- [ ] Only in-scope files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- `QueryableExtensions` turns out to be used elsewhere with filters that rely on the old throw-on-mismatch behavior (`grep -rn "ApplyQueryParams\|ApplyFilters\|ApplySorting" src`). Report the call sites.
- `OrderDto` mapping needs navigation properties (check `OrderMapper.cs`). If so, `AsNoTracking()` without `Include` would map nulls. Add the needed `Include`s and report them.

## Maintenance notes

- Add new filterable fields to `OrderFilterableProperties`. Never pass a client property name through unfiltered.
- When the frontend is ready for a total count, introduce `PagedResult<OrderDto>` behind a new action rather than changing this one.
