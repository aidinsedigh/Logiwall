# Cycle 011 — plan

## Contents

- L33 Git workflow
- L37 STOP conditions
- L49 Commands you will need
- L56 Tasks
  - L58 Task 1: Extend `OrderState`
  - L69 Task 2: Resolve postal providers through DI
  - L83 Task 3: Rewrite `GetOptionsAsync` to be idempotent and provider-isolated
  - L116 Task 4: Record the selection in `SelectOptionAndPaymentAsync`
  - L131 Task 5: Transactional registration and profile update
  - L147 Task 6: Fake provider and tests
- L171 Test plan
- L175 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems Logiwall-API/src/Logiwall.Infrastructure/Services/People/PersonService.cs Logiwall-API/src/Logiwall.Infrastructure/Providers/PostalServices Logiwall-API/src/Logiwall.Infrastructure/Services/DependencyInjection.cs Logiwall-API/src/Logiwall.Shared/Enums/OrderState.cs Logiwall-API/src/Logiwall.Server/Controllers/People/PersonController.cs`
> Expected from earlier plans: 002 (ownership predicates and new `userId`
> parameters), 003 (options in providers; `AddInfrastructure(IConfiguration)`),
> 008 (named `sms` HttpClient), and 009 (`ILogger` in the services, no `ex.Message`).
> Find the excerpts below by content. If the reflection-based provider loop in
> `GetOptionsAsync` is already gone, STOP.

## Git workflow

- Branch `advisor/011-order-quote-flow`. Commit per step. Do NOT push.

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

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Tests | `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Tasks

### Task 1: Extend `OrderState`

Append to `OrderState`:
```csharp
[Description("در انتظار پرداخت")]
PaymentPending = 3,
[Description("پرداخت ناموفق")]
PaymentFailed = 4,
```
**Verify**: build → exit 0.

### Task 2: Resolve postal providers through DI

1. In `PostalServiceBase`, add `public abstract ProviderType ProviderType { get; }`. Delete the stub
   `Payment(...)` method, and the stale `//todo should be abstract` comment. Before deleting
   `Payment`, check that nothing calls it: `grep -rn "\.Payment(" src`.
2. In `MyTipaxProvider`, add `public override ProviderType ProviderType => ProviderType.MyTipax;`.
3. In `DependencyInjection`, replace the block quoted in "Current state" with
   `services.TryAddEnumerable(ServiceDescriptor.Transient<PostalServiceBase, MyTipaxProvider>());`.
4. Remove the `Func<ProviderType, PostalServiceBase>` constructor parameter and field from `PaymentService`.
5. In `OrderService`, replace the `IServiceProvider` constructor parameter with
   `IEnumerable<PostalServiceBase> postalProviders`, and remove `using System.Reflection;` if unused.

**Verify**: build fails only inside `GetOptionsAsync` (which still uses reflection). Task 3 fixes that.

### Task 3: Rewrite `GetOptionsAsync` to be idempotent and provider-isolated

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

### Task 4: Record the selection in `SelectOptionAndPaymentAsync`

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

### Task 5: Transactional registration and profile update

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

### Task 6: Fake provider and tests

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

See Task 6. All earlier tests must still pass.

## Done criteria

- [ ] Build exits 0, and all integration tests pass (≥4 new)
- [ ] `grep -rn "Func<ProviderType, PostalServiceBase>" src` → no output
- [ ] `grep -rn "Activator.CreateInstance" src/Logiwall.Infrastructure` → no output
- [ ] `OrderState` has values 0–4, with 0–2 unchanged
- [ ] Only in-scope files changed
- [ ] this cycle's `tasks.md` status row updated
