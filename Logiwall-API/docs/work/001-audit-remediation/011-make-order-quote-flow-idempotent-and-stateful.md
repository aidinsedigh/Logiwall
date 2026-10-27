# Plan 011: Quoting an order is idempotent, providers come from DI, order state and history are recorded, and multi-step writes are transactional

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems Logiwall-API/src/Logiwall.Infrastructure/Services/People/PersonService.cs Logiwall-API/src/Logiwall.Infrastructure/Providers/PostalServices Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs Logiwall-API/src/Logiwall.Shared/Enums/OrderState.cs Logiwall-API/src/Logiwall.Server/Controllers/People/PersonController.cs`
> Expected from earlier plans: 002 (ownership predicates and new `userId`
> parameters), 003 (options in providers; `AddInfrastructure(IConfiguration)`),
> 008 (named `sms` HttpClient), and 009 (`ILogger` in the services, no `ex.Message`).
> Find the excerpts below by content. If the reflection-based provider loop in
> `GetOptionsAsync` is already gone, STOP.

## Status

- **Priority**: P2
- **Effort**: M
- **Risk**: MED (changes visible behavior: option IDs are replaced on each re-quote)
- **Depends on**: docs/work/001-audit-remediation/002-enforce-resource-ownership.md, docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md, docs/work/001-audit-remediation/009-harden-production-defaults.md
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

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

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

## Git workflow

- Branch `advisor/011-order-quote-flow`. Commit per step. Do NOT push.

## Steps

### Step 1: Extend `OrderState`

Append to `OrderState`:
```csharp
[Description("در انتظار پرداخت")]
PaymentPending = 3,
[Description("پرداخت ناموفق")]
PaymentFailed = 4,
```
**Verify**: build → exit 0.

### Step 2: Resolve postal providers through DI

1. In `PostalServiceBase`, add `public abstract ProviderType ProviderType { get; }`. Delete the stub
   `Payment(...)` method, and the stale `//todo should be abstract` comment. Before deleting
   `Payment`, check that nothing calls it: `grep -rn "\.Payment(" src`.
2. In `MyTipaxProvider`, add `public override ProviderType ProviderType => ProviderType.MyTipax;`.
3. In `DependencyInjection`, replace the block quoted in "Current state" with
   `services.TryAddEnumerable(ServiceDescriptor.Transient<PostalServiceBase, MyTipaxProvider>());`.
4. Remove the `Func<ProviderType, PostalServiceBase>` constructor parameter and field from `PaymentService`.
5. In `OrderService`, replace the `IServiceProvider` constructor parameter with
   `IEnumerable<PostalServiceBase> postalProviders`, and remove `using System.Reflection;` if unused.

**Verify**: build fails only inside `GetOptionsAsync` (which still uses reflection). Step 3 fixes that.

### Step 3: Rewrite `GetOptionsAsync` to be idempotent and provider-isolated

Keep the signature from plan 002 (`GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId)`)
and keep the ownership-filtered order load and its null branch. Then:

1. **State guard.** If `order.State` is `(short)OrderState.Paid` or `(short)OrderState.PaymentPending`,
   return `Succuss = false, Message = "Order can no longer be re-quoted."`.
2. **Mapper codes in one query.** Load the codes for `OriginCity`, `DestinationCity` and
   `DestinationCountry` once, as a list of `(StaticDataId, ProviderType, Code)`, and look them up in memory.
   Filter by `ProviderType == provider.ProviderType` per provider (this resolves the `//todo check providerType`).
3. **Query each provider in isolation.** For each `provider` in `postalProviders`, build the
   same `OrderInquiryRequestDto` as today (keep the parcel mapping exactly, including the
   `ParcelValue/10`; plan 010 decides on units). Wrap the call in `try/catch (Exception ex)`,
   log with `logger.LogWarning(ex, "Quote from {Provider} failed", provider.GetType().Name)`,
   and continue with the next provider.
4. **Replace, don't append.** Collect all successful results first. Then:
   ```csharp
   await using var tx = await appDbContext.Database.BeginTransactionAsync();
   var old = await appDbContext.OrderInqueryOptions.Where(t => t.OrderId == order.Id).ToListAsync();
   appDbContext.OrderInqueryOptions.RemoveRange(old);
   appDbContext.OrderInqueryOptions.AddRange(newOptions);
   order.State = (short)OrderState.Created;
   await appDbContext.SaveChangesAsync();
   await tx.CommitAsync();
   ```
   `ServiceProviderId` for each provider: look it up with
   `appDbContext.ServiceProviders.FirstOrDefaultAsync(t => t.Name == provider.GetType().Name)`.
   If no row is found, log a warning and skip that provider's options; don't throw.
5. If **every** provider failed, return the existing Persian failure message, and don't delete the old options.
6. Return the options projected exactly as today (`OrderInqueryOptionsDto`).

**Verify**: build → exit 0; `grep -n "Activator.CreateInstance\|Assembly.GetAssembly" src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` → no output.

### Step 4: Record the selection in `SelectOptionAndPaymentAsync`

After the order and option are loaded and validated (plan 002), and **before**
calling the gateway:
1. If `order.State` is `Paid`, return `Succuss = false, Message = "Order is already paid."`.
2. In one `SaveChangesAsync`:
   - set `Choosen = true` on the selected option, and `false` on every other option of the order;
   - copy `ServiceAmount`, `PackagingAmount`, `InsuranceAmount`, `Tax`, `ServiceType`, `ServiceSLA`
     and `ServiceProviderId` from the option onto the order;
   - set `order.State = (short)OrderState.SelectedServiceProvider` and `order.ModificationDate = DateTimeOffset.Now`;
   - add `new OrderStatus { OrderId = order.Id, Status = (int)OrderState.SelectedServiceProvider, ServiceProviderId = option.ServiceProviderId, CityId = order.OriginCity, Description = "Service option selected" }`.
3. Then call the gateway as today. Don't change the gateway call or its response handling (plan 012).

**Verify**: build → exit 0.

### Step 5: Transactional registration and profile update

1. In `PersonController.Register`: wrap everything from `CreatePersonAsync` through
   `ResetPasswordAsync` in `await using var tx = await DbContext.Database.BeginTransactionAsync(cancellationToken);`.
   `DbContext` is the protected `AppDbContext` field inherited from `AppControllerBase`.
   Call `await tx.CommitAsync(cancellationToken)` only after `ResetPasswordAsync` succeeds. On any
   failure path, return as today; the `await using` rolls back.
2. In `PersonService.UpdateUserProfileAsync`: replace the email lines with
   ```csharp
   var setEmail = await userManager.SetEmailAsync(user, requestDto.Email);
   if (setEmail.Succeeded is false) { return new ServiceResponse<UserProfileDto> { Data = requestDto, Succuss = false, Message = "Invalid email" }; }
   ```
   Wrap that call and the later person/personal-info `SaveChangesAsync` in one transaction on `dbContext`.

**Verify**: build → exit 0; `grep -n "Normalize()" src/Logiwall.Infrastructure/Services/People/PersonService.cs` → no output.

### Step 6: Fake provider and tests

1. Create `tests/Logiwall.IntegrationTests/Infrastructure/FakePostalProvider.cs`:
   `public class FakePostalProvider(IAppSettingService s, IHttpProvider h) : PostalServiceBase(s, h)`.
   `ProviderType => ProviderType.MyTipax`. `GetOrderInquiryAsync` returns two `OrderInquiryOptionDto`
   with string amounts (`"1000"`, `"0"`, `"100"`, `"1100"` for SpecialService/PackageService/Tax/FinalAmount)
   and `DelivereDate = "1"`. Add a static `bool Fail` switch that makes it throw.
   (If `PostalServiceBase`'s constructor is `protected`, a primary constructor still works.)
2. In `LogiwallApiFactory.ConfigureTestServices`:
   `s.RemoveAll<PostalServiceBase>(); s.AddTransient<PostalServiceBase, FakePostalProvider>();`
3. The fixture must ensure a `ServiceProviders` row with `Name = "FakePostalProvider"` exists.
   Check `AppDbContextModelSnapshot.cs` to see whether `ServiceProvider.Id` is database-generated;
   if it isn't, insert it with an unused explicit `Id`.
4. `tests/Logiwall.IntegrationTests/Orders/OrderQuoteFlowTests.cs`:
   - `GetOptions_twice_returns_same_count_not_double`: 2 options, then still 2.
   - `GetOptions_with_failing_provider_keeps_previous_options`: quote once, set `Fail = true`, quote again → 400, and the DB still has 2 options. Reset `Fail` in `finally`.
   - `SelectOption_marks_choice_and_state`: quote, then select option 1. Because the gateway is not configured in tests, the response may be 400, which is fine. Assert in the DB: the option has `Choosen = true`, the other `false`; `order.State == 1`; and one `OrderStatuses` row exists.
   - `Register_rolls_back_person_when_password_reset_fails`: register with a password that the
     Identity policy rejects but the DTO accepts. Check `appsettings.json` `IdentitySettings` for the
     policy. If no such password exists (both require only length ≥ 6), **skip this test** with
     `[Fact(Skip="no policy gap to exercise")]` and say so in your report.

**Verify**: `dotnet test tests/Logiwall.IntegrationTests` → all pass.

## Test plan

See Step 6. All earlier tests must still pass.

## Done criteria

- [ ] Build exits 0, and all integration tests pass (≥4 new)
- [ ] `grep -rn "Func<ProviderType, PostalServiceBase>" src` → no output
- [ ] `grep -rn "Activator.CreateInstance" src/Logiwall.Infrastructure` → no output
- [ ] `OrderState` has values 0–4, with 0–2 unchanged
- [ ] Only in-scope files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- A **migration** turns out to be required (for example if `dotnet ef migrations add Check`
  shows model changes). This plan should not change the schema; enum values are plain shorts.
  Report it, and delete the check migration.
- `ServiceProviders.Name` for MyTipax is not `"MyTipaxProvider"`. Check the seed in
  `20240921075422_AddCitiesAndOtherConstants.cs`, and **look only at the ServiceProviders
  insert; the other rows there are credentials**. If the name is different, the
  `GetType().Name` lookup would break production quotes.
- The frontend depends on option IDs staying stable across `GetOptions` calls
  (`grep -rn "orderInqueryOptionId" Logiwall-Front/src`). If it caches IDs across page loads, report before proceeding.

## Maintenance notes

- Adding a carrier now means: one `PostalServiceBase` subclass, one
  `TryAddEnumerable` line, a `ServiceProviders` row, and `StaticDataMapper` rows for its `ProviderType`.
- Providers are still queried sequentially. Parallelizing with per-provider timeouts is a follow-up for when there are two or more providers.
- Plan 012 adds the `PaymentPending → Paid/PaymentFailed` transitions on top of this.
