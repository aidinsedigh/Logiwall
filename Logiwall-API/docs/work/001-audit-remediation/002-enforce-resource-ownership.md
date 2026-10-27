# Plan 002: Customer contacts, orders, options and payments only resolve when they belong to the caller

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Application/Abstractions Logiwall-API/src/Logiwall.Infrastructure/Services Logiwall-API/src/Logiwall.Server/Controllers`
> If any in-scope file changed since this plan was written, compare the
> "Current state" excerpts against the live code before proceeding. Changes
> made by plan 001 to `AuthenticationService.cs` are expected and irrelevant
> here. Any other mismatch is a STOP condition.

## Status

- **Priority**: P1 (security: insecure direct object references)
- **Effort**: S
- **Risk**: LOW
- **Depends on**: none
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Several authenticated endpoints load a record by the integer ID the client sends,
without checking that it belongs to the caller. Because IDs are sequential, any
logged-in user can:
- read every customer's saved addresses, phone numbers and coordinates
  (`Person/GetCustomerContact?id=N`);
- **overwrite** those addresses (`Person/UpdateCustomerContact`), which redirects
  shipments;
- trigger paid courier price inquiries on other customers' orders
  (`Order/GetOptions`);
- start a payment for an order using an option row from a *different, cheaper*
  order (`Order/SelectOption`);
- record payments against other customers' orders (`Payment/UpdatePaymentOrderStatus`).

After this plan, each of these lookups includes "belongs to the current user".
A foreign or missing ID returns the same failure response, so callers can't
probe which IDs exist.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. It contains `Logiwall-API/` (this
  backend) and `Logiwall-Front/` (React, do not touch). Paths are relative to
  `Logiwall-API/`, and commands run from `Logiwall-API/`.
- Stack: .NET 8, ASP.NET Core, EF Core 8 SQL Server. Controllers are `partial`
  classes that receive dependencies through `[AutoInject]` fields (a source
  generator). Services use constructor injection and are registered in
  `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`.
- Controllers get the caller's ID with `User.GetUserId()`
  (`src/Logiwall.Shared/Extensions/ClaimsPrincipalExtensions.cs`). Every
  controller except `IdentityController` and `CommonController` requires
  authentication, set globally in `Program.Middlewares.cs:105`.
- Ownership chain in the domain: `Order.CustomerId → Customer.PersonId →
  Person.UserId`, and `CustomerContact.CustomerId → Customer`. The navigation
  properties are `Order.Customer`, `Customer.Person`, `CustomerContact.Customer`
  and `Person.UserId`.
- Services return `ServiceResponse<T>` with the misspelled `Succuss` flag. **Do
  not rename it** (wire contract).
- Controller action signatures are mirrored by interfaces in
  `src/Client/Logiwall.Client.Core/Controllers/**` (e.g. `IOrderController`).
  **Do not change controller action signatures.** Change only the service
  interfaces in `src/Logiwall.Application/Abstractions/**`.
- Build only `src/Logiwall.Server/Logiwall.Server.csproj`, never `Logiwall.sln`
  (it includes MAUI).

## Current state

`src/Logiwall.Infrastructure/Services/People/CustomerService.cs`:
- Line 49: `var contactInfo = await appDbContext.CustomerContacts.FirstOrDefaultAsync(t => t.Id == id);` (no owner filter)
- Line 111: `var contactInfo = await appDbContext.CustomerContacts.FirstOrDefaultAsync(t => t.Id == requestDto.Id);` then dereferences `contactInfo` without a null check (line 112).
- Lines 19 and 85: `var person = await appDbContext.People.Select(t => new { t.UserId, CustomerId = t.Customer.Id }).FirstOrDefaultAsync(t => t.UserId == ...);` then dereferences `person.CustomerId` without a null check.
- The existing failure shape (lines 52-57):
  ```csharp
  return new ServiceResponse<CustomerContactResponseDto>
  {
      Data = null,
      Succuss = false,
      Message = "There is not contact info with this id"
  };
  ```

`src/Logiwall.Application/Abstractions/People/ICustomerService.cs`:
```csharp
Task<ServiceResponse<bool>> UpdateCustomerContactAsync(UpdateCustomerContactRequestDto requestDto);
Task<ServiceResponse<CustomerContactResponseDto>> GetCustomerContactByIdAsync(int id);
```

`src/Logiwall.Server/Controllers/People/PersonController.cs`:
- Line 111: `var result = await customerService.GetCustomerContactByIdAsync(id);`
- Line 138: `var result = await customerService.UpdateCustomerContactAsync(requestDto);`

`src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`:
- Line 74: `public async Task<ServiceResponse<IList<OrderInqueryOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto)`
- Line 78: `var order = await appDbContext.Orders.Include(t => t.OrderParcels).FirstOrDefaultAsync(t => t.Id == requestDto.OrderId);`. There is no null check, so a null `order` throws later and the exception is swallowed at line 157.
- Line 211: `var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId);` (in `SelectOptionAndPaymentAsync`; `userId` is a parameter but is not used here)
- Line 223: `var orderInquiry = await appDbContext.OrderInqueryOptions.FirstOrDefaultAsync(t => t.Id == requestDto.OrderInqueryOptionId);`. Line 227 dereferences `orderInquiry.FinalAmount` with no null check and no check that it belongs to `order`.
- The failure shape used at lines 215-220:
  ```csharp
  return new ServiceResponse<SelectOptionResponseDto>
  {
      Data = null,
      Succuss = false,
      Message = "Invalid OrderId"
  };
  ```

`src/Logiwall.Application/Abstractions/OrderItems/IOrderService.cs:9`:
`Task<ServiceResponse<IList<OrderInqueryOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto);`

`src/Logiwall.Server/Controllers/Order/OrderController.cs:35-37`:
```csharp
public async Task<ActionResult> GetOptions([FromQuery]GetOptionnsRequestDto requestDto, CancellationToken cancellationToken)
{
    var result = await orderService.GetOptionsAsync(requestDto);
```

`src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs`:
- Line 22: `PaymentOrderStatusAsync(PaymentRequestDto requestDto, int userId)`. `userId` is never used.
- Line 26: `var order = await appDbContext.Orders.FirstOrDefaultAsync(t => t.Id == requestDto.OrderId);`
- Line 60: `var payment = await appDbContext.OrderPayments.FirstOrDefaultAsync(t => t.PayTrackingCode == requestDto.PayTrackingCode);` (searches every order's payments)

## Commands you will need

| Purpose | Command (from `Logiwall-API/`) | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0, "Build succeeded" (≈40 pre-existing warnings are normal) |

## Scope

**In scope**:
- `src/Logiwall.Application/Abstractions/People/ICustomerService.cs`
- `src/Logiwall.Application/Abstractions/OrderItems/IOrderService.cs`
- `src/Logiwall.Infrastructure/Services/People/CustomerService.cs`
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` (only the lines named in the steps)
- `src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs` (only lines 26 and 60)
- `src/Logiwall.Server/Controllers/People/PersonController.cs` (only the two service calls)
- `src/Logiwall.Server/Controllers/Order/OrderController.cs` (only the `GetOptions` service call)

**Out of scope**:
- `PaymentService` still trusts a client-reported "success". Plan 012 replaces
  that whole flow, so only add the ownership filters here.
- `GetOptionsAsync` inserting duplicate options (plan 011).
- `GetOrderListAsync`. It already filters by the caller's customer (line 181).
- DTOs in `src/Logiwall.Shared/Dtos/**`. They are shared with clients. Do not add
  or remove properties. `UpdateCustomerContactRequestDto.CustomerId` should
  simply be ignored by the service.
- Interfaces under `src/Client/**`.

## Git workflow

- Branch: `advisor/002-enforce-resource-ownership`
- Commit messages: short and imperative, e.g. `Scope customer contacts to the calling user`.
- Do NOT push.

## Steps

### Step 1: Scope customer-contact read and update to the caller

1. In `ICustomerService.cs`, change the two signatures to:
   ```csharp
   Task<ServiceResponse<bool>> UpdateCustomerContactAsync(UpdateCustomerContactRequestDto requestDto, int userId);
   Task<ServiceResponse<CustomerContactResponseDto>> GetCustomerContactByIdAsync(int id, int userId);
   ```
2. In `CustomerService.cs`:
   - `GetCustomerContactByIdAsync(int id, int userId)`: query
     `appDbContext.CustomerContacts.FirstOrDefaultAsync(t => t.Id == id && t.Customer.Person.UserId == userId)`.
     Keep the existing null branch and its message unchanged.
   - `UpdateCustomerContactAsync(UpdateCustomerContactRequestDto requestDto, int userId)`:
     query with `t.Id == requestDto.Id && t.Customer.Person.UserId == userId`.
     If the result is null, return
     `new ServiceResponse<bool> { Data = false, Succuss = false, Message = "There is not contact info with this id" }`
     before touching any field. Do not read `requestDto.CustomerId`.
   - `CreateCustomerContactAsync` and `GetCustomerContactListAsync`: after
     loading `person`, add `if (person is null)` and return
     `Succuss = false, Message = "User is not registered"`
     (the same text `OrderService.cs:207` uses). For the list method the return
     type is `ServiceResponse<IEnumerable<CustomerContactResponseDto>>`, with `Data = null`.
3. In `PersonController.cs`, pass `User.GetUserId()` as the new last argument
   to both calls (lines 111 and 138).

**Verify**:
- `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` → exit 0.
- `grep -n "t.Id == id)" src/Logiwall.Infrastructure/Services/People/CustomerService.cs` → no output.
- `grep -c "Person.UserId == userId" src/Logiwall.Infrastructure/Services/People/CustomerService.cs` → `2`.

### Step 2: Scope order options and option selection to the caller

1. `IOrderService.cs`: change the signature to
   `Task<ServiceResponse<IList<OrderInqueryOptionsDto>>> GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId);`
2. `OrderController.GetOptions`: call `orderService.GetOptionsAsync(requestDto, User.GetUserId())`.
3. `OrderService.GetOptionsAsync(GetOptionnsRequestDto requestDto, int userId)`:
   - Line 78: change the predicate to `t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId`.
   - Immediately after it, add:
     ```csharp
     if (order is null)
     {
         return new ServiceResponse<IList<OrderInqueryOptionsDto>>
         {
             Data = null,
             Succuss = false,
             Message = "Invalid OrderId"
         };
     }
     ```
4. `OrderService.SelectOptionAndPaymentAsync`:
   - Line 211: change the predicate to `t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId`. The existing null branch stays.
   - Line 223: change the predicate to `t.Id == requestDto.OrderInqueryOptionId && t.OrderId == order.Id`.
   - Immediately after it, add a null branch that returns
     `ServiceResponse<SelectOptionResponseDto>` with `Succuss = false`, `Message = "Invalid OrderInqueryOptionId"`.

**Verify**:
- Build → exit 0.
- `grep -c "Customer.Person.UserId == userId" src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` → `2`.
- `grep -n "t.OrderId == order.Id" src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` → 1 match.

### Step 3: Scope payment lookups to the caller (ownership only)

In `PaymentService.PaymentOrderStatusAsync`:
- Line 26: change the predicate to `t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId`. The existing null branch stays.
- Line 60: change the predicate to `t.PayTrackingCode == requestDto.PayTrackingCode && t.OrderId == order.Id`.

Leave everything else in this file alone. Plan 012 replaces it.

**Verify**:
- Build → exit 0.
- `grep -n "userId" src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs` → at least 2 matches (the parameter and the predicate).

## Test plan

There is no test project yet. Plan 005 creates the integration test harness and
**must** include these cases. They are listed here so the reviewer can check
that plan 005 has them:
1. User A creates a contact. User B calls `GetCustomerContact?id=<A's id>` and gets 400 with `Succuss=false`.
2. User B calls `UpdateCustomerContact` with A's contact ID and gets 400. A's row is unchanged in the DB.
3. User B calls `Order/GetOptions?orderId=<A's order>` and gets 400 "Invalid OrderId".
4. User A calls `SelectOption` with their own order and an option ID that belongs to another order, and gets 400 "Invalid OrderInqueryOptionId".

For this plan, verification is the build plus the greps above.

## Done criteria

- [ ] `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` exits 0
- [ ] `grep -rn "FirstOrDefaultAsync(t => t.Id == id)" src/Logiwall.Infrastructure/Services/People` → no matches
- [ ] `grep -rn "FirstOrDefaultAsync(t => t.Id == requestDto.OrderId)" src/Logiwall.Infrastructure/Services` → no matches
- [ ] `grep -rn "FirstOrDefaultAsync(t => t.Id == requestDto.OrderInqueryOptionId)" src/Logiwall.Infrastructure` → no matches
- [ ] No file under `src/Logiwall.Shared/Dtos` or `src/Client` is modified (`git status`)
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

- The navigation `t.Customer.Person.UserId` doesn't compile. That would mean the
  domain model differs from this plan's assumption (`Order.Customer`,
  `Customer.Person`, `Person.UserId`).
- A controller in `src/Client/Logiwall.Client.Core/Controllers` declares a
  method that calls the service directly, so changing a service signature
  breaks the client build. Report it; don't change client code.
- Any of the excerpts above don't match the live code.

## Maintenance notes

- Every new endpoint that takes an entity ID must include the ownership
  predicate. Reviewers should grep new service methods for
  `FirstOrDefaultAsync(t => t.Id ==` without a `UserId` clause.
- An admin or back-office role, if one is ever added, needs a separate,
  explicitly authorized path. Don't loosen these predicates.
- `UpdateCustomerContactRequestDto.CustomerId` is now ignored. It could be
  removed from the DTO later, in coordination with the frontend.
