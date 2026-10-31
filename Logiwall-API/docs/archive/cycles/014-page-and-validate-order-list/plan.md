# Cycle 014 — plan

## Contents

- L26 Git workflow
- L30 STOP conditions
- L35 Commands you will need
- L43 Tasks
  - L45 Task 1: Make filters type-safe, allow-listed and aliasable
  - L80 Task 2: Rewrite `GetOrderListAsync`
  - L103 Task 3: Tests
- L119 Test plan
- L123 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Shared/Extensions/QueryableExtensions.cs Logiwall-API/src/Logiwall.Shared/Dtos/Global Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs Logiwall-API/src/Logiwall.Server/Controllers/Order/OrderController.cs`
> `OrderService.cs` will show changes from plans 002/009/011/012 in **other** methods. `GetOrderListAsync` itself should be unchanged. If it has changed, STOP.

## Git workflow

- Branch `advisor/014-order-list`. Commit per step. Do NOT push.

## STOP conditions

- `QueryableExtensions` turns out to be used elsewhere with filters that rely on the old throw-on-mismatch behavior (`grep -rn "ApplyQueryParams\|ApplyFilters\|ApplySorting" src`). Report the call sites.
- `OrderDto` mapping needs navigation properties (check `OrderMapper.cs`). If so, `AsNoTracking()` without `Include` would map nulls. Add the needed `Include`s and report them.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Unit tests | `dotnet test tests/Logiwall.UnitTests` | all pass |
| Integration tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Tasks

### Task 1: Make filters type-safe, allow-listed and aliasable

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

### Task 2: Rewrite `GetOrderListAsync`

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
- Pagination: if `HasPagination`, use `ApplyPagination` with the clamped values from Task 1. Otherwise apply `.Take(100)` (the newest 100 orders, which is what the home page wants).
- Materialize with `await query.ToListAsync()`, then map with `.Select(o => o.Map()).ToList()`.
  (Mapperly maps in memory, so map *after* `ToListAsync`.)

**Verify**: build → exit 0; `grep -n "foreach (var order in query)" src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` → no output.

### Task 3: Tests

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

See Task 3.

## Done criteria

- [ ] Build exits 0; unit tests (≥9) and integration tests pass
- [ ] Greps in Task 2 return no output
- [ ] Response shape unchanged: `ServiceResponse<IList<OrderDto>>` (check the controller and service signatures haven't changed)
- [ ] Only in-scope files changed
- [ ] this cycle's `tasks.md` status row updated
