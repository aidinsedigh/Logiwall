# Cycle 011 — Quoting an order is idempotent, providers come from DI, order state and history are recorded, and multi-step writes are transactional: spec

## Contents

- L19 Summary
- L28 Why this matters
- L38 Repo facts you need
- L53 Current state
- L129 Scope
- L149 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 011 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P2
- **Effort**: M
- **Risk**: MED (changes visible behavior: option IDs are replaced on each re-quote)
- **Depends on**: docs/archive/cycles/002-enforce-resource-ownership/, docs/archive/cycles/005-establish-test-and-ci-baseline/, docs/archive/cycles/009-harden-production-defaults/
- **Category**: bug
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

The core product flow is create order → get courier quotes → select a quote →
pay. Today:
- `GET Order/GetOptions` **inserts a fresh set of quote rows on every call** and returns all accumulated rows, so refreshing the page duplicates quotes.
- Postal providers are found by **assembly reflection plus service-locator construction**. That bypasses DI, so a missing dependency silently becomes `null`, and tests can't substitute a fake courier.
- One failing provider fails the whole quote.
- `Order.State` and the `OrderStatuses` history table are **never written**. A chosen quote is never marked `Choosen`, so nothing downstream (payment verification in plan 012, shipment booking) knows which quote the customer accepted or what it cost.
- `Person/Register` and profile updates do several separate saves with no transaction, so a partial failure leaves half-registered users.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- .NET 8, EF Core 8 on SQL Server. `AppDbContext` (namespace `Logiwall.Server.Data`,
  file `src/Logiwall.Infrastructure/Data/AppDbContext.cs`) is **scoped**. `UserManager<User>` and
  every service share the same instance within a request, so one
  `Database.BeginTransactionAsync()` covers Identity and domain writes. No
  retrying execution strategy is configured, so explicit transactions are allowed.
- Enums are stored as `short` columns. `Order.State` is a `short`. `OrderState`
  (`src/Logiwall.Shared/Enums/OrderState.cs`) is a `short` enum. **Append new
  values only; never renumber.**
- `ServiceResponse<T>`/`Succuss` is a misspelled wire contract. Don't rename it.
- Test harness: `tests/Logiwall.IntegrationTests` (plan 005). See
  `Orders/OrderOwnershipTests.cs`, which inserts orders directly through `AppDbContext`.

## Current state

`src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`. The constructor takes `AppDbContext`, `ISep724Provider` and **`IServiceProvider`**.
`GetOptionsAsync` (the core of it, abridged):
```csharp
var postalServiceBase = typeof(PostalServiceBase);
var postalProviders = Assembly.GetAssembly(postalServiceBase).GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && postalServiceBase.IsAssignableFrom(t)).ToList();

var lst = new List<OrderInqueryOptionsDto>();
var dataMapper = appDbContext.StaticDataMappers
    .Where(t => t.StaticDataId == order.OriginCity || t.StaticDataId == order.DestinationCity
        || t.StaticDataId == order.DestinationCountry);
foreach (var postalProvider in postalProviders)
{
    var constructor = postalProvider.GetConstructors().FirstOrDefault();
    var parameters = constructor.GetParameters();
    var parameterInstances = parameters.Select(param => serviceProvider.GetService(param.ParameterType)).ToArray();

    var instance = (PostalServiceBase)Activator.CreateInstance(postalProvider, parameterInstances);
    var options = await instance.GetOrderInquiryAsync(new OrderInquiryRequestDto
    {
        CountryId = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.DestinationCountry))?.Code, //todo check providerType
        OriginCity = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.OriginCity))?.Code,
        DestinationCity = (await dataMapper.FirstOrDefaultAsync(t => t.StaticDataId == order.DestinationCity))?.Code,
        Parcels = order.OrderParcels.Select(t => new OrderInquiryParcelDto { ... }).ToList(),
    });

    //todo add to database
    if (options is not null)
    {
        var serviceProvider = await appDbContext.ServiceProviders.FirstOrDefaultAsync(t => t.Name == postalProvider.Name);
        appDbContext.AddRange(options.Select(t => new OrderInqueryOption { ... OrderId = requestDto.OrderId, ServiceProviderId = serviceProvider.Id, Choosen = false, ... }));
        await appDbContext.SaveChangesAsync();
    }
}
```
After the loop, all `OrderInqueryOptions` for the order are projected to `OrderInqueryOptionsDto` and returned.

`SelectOptionAndPaymentAsync` loads the order and option (with plan 002's
ownership predicates), calls `sep724Provider.GetPaymentTokenAsync(...)` and returns the link. It never marks the option or the order.

`src/Logiwall.Infrastructure/Services/DependencyInjection.cs:21-30`:
```csharp
//services.TryAddTransient<IPostalServiceBase, PostalServiceBase>();//todo
services.TryAddTransient<MyTipaxProvider>();
services.AddTransient<Func<ProviderType, PostalServiceBase>>(serviceProvider => ProviderType =>
{
    return ProviderType switch
    {
        ProviderType.MyTipax => serviceProvider.GetService<MyTipaxProvider>(),
        _ => throw new ArgumentException("Invalid Postal Service type", nameof(ProviderType)),
    };
});
```
`PaymentService`'s constructor injects that `Func<ProviderType, PostalServiceBase>` and never uses it.

`src/Logiwall.Infrastructure/Providers/PostalServices/PostalServiceBase.cs`: an abstract class
with `GetOrderInquiryAsync`, and a stub `Payment(PaymentRequestDto)` that always returns `Success`.

`src/Logiwall.Domain/Entities/OrderItems/OrderStatus.cs`: `Status` (int), `ServiceProviderId` (int), `CityId` (short), `Description` (≤500), `CreationDate` (column `EventDate`), `OrderId`.

`src/Logiwall.Domain/Entities/Global/StaticDataMapper.cs`: `Code`, `ProviderType`, `StaticDataId`.

`src/Logiwall.Shared/Enums/OrderState.cs`: `Created = 0, SelectedServiceProvider = 1, Paid = 2`.

`src/Logiwall.Server/Controllers/People/PersonController.cs`, `Register`: calls `personService.CreatePersonAsync(...)` (which saves), then `userManager.GeneratePasswordResetTokenAsync` and `ResetPasswordAsync`. If the reset fails, the person row stays.

`src/Logiwall.Infrastructure/Services/People/PersonService.cs`, `UpdateUserProfileAsync`:
```csharp
user.Email = requestDto.Email;
user.NormalizedEmail = requestDto.Email?.Normalize();
await userManager.UpdateAsync(user);
```
This is Unicode normalization, not Identity's normalization, and the result is ignored. A separate `SaveChangesAsync` follows.

## Scope

**In scope**:
- `src/Logiwall.Shared/Enums/OrderState.cs` (append values)
- `src/Logiwall.Infrastructure/Providers/PostalServices/PostalServiceBase.cs`, `MyTipaxProvider.cs`
- `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` (`GetOptionsAsync`, `SelectOptionAndPaymentAsync`, constructor)
- `src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs` (constructor only: remove the unused `Func` parameter)
- `src/Logiwall.Infrastructure/Services/People/PersonService.cs` (`UpdateUserProfileAsync`)
- `src/Logiwall.Server/Controllers/People/PersonController.cs` (`Register`)
- `tests/Logiwall.IntegrationTests/Infrastructure/FakePostalProvider.cs` (create), `LogiwallApiFactory.cs`
- `tests/Logiwall.IntegrationTests/Orders/OrderQuoteFlowTests.cs` (create)

**Out of scope**:
- Payment verification and the `Paid` transition (plan 012).
- `GetOrderListAsync` (plan 014).
- Shipment booking and tracking (a direction item, not planned).
- Adding a `CreationDate` or TTL to quotes. Re-quoting replaces them; that is enough for now.
- The `IPostalServiceBase` interface in Application (wrong namespace, `...PaymentGateways`). Plan 015 cleans up names.

## Maintenance notes

- Adding a carrier now means: one `PostalServiceBase` subclass, one
  `TryAddEnumerable` line, a `ServiceProviders` row, and `StaticDataMapper` rows for its `ProviderType`.
- Providers are still queried sequentially. Parallelizing with per-provider timeouts is a follow-up for when there are two or more providers.
- Plan 012 adds the `PaymentPending → Paid/PaymentFailed` transitions on top of this.
