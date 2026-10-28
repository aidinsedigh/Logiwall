# Cycle 002 — Customer contacts, orders, options and payments only resolve when they belong to the caller: spec

## Contents

- L19 Summary
- L28 Why this matters
- L47 Repo facts you need
- L73 Current state
- L129 Scope
- L150 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 002 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

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

## Maintenance notes

- Every new endpoint that takes an entity ID must include the ownership
  predicate. Reviewers should grep new service methods for
  `FirstOrDefaultAsync(t => t.Id ==` without a `UserId` clause.
- An admin or back-office role, if one is ever added, needs a separate,
  explicitly authorized path. Don't loosen these predicates.
- `UpdateCustomerContactRequestDto.CustomerId` is now ignored. It could be
  removed from the DTO later, in coordination with the frontend.
