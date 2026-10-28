# Cycle 002 — plan

## Contents

- L29 Git workflow
- L35 STOP conditions
- L45 Commands you will need
- L51 Tasks
  - L53 Task 1: Scope customer-contact read and update to the caller
  - L82 Task 2: Scope order options and option selection to the caller
  - L112 Task 3: Scope payment lookups to the caller (ownership only)
- L124 Test plan
- L136 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Application/Abstractions Logiwall-API/src/Logiwall.Infrastructure/Services Logiwall-API/src/Logiwall.Server/Controllers`
> If any in-scope file changed since this plan was written, compare the
> "Current state" excerpts against the live code before proceeding. Changes
> made by plan 001 to `AuthenticationService.cs` are expected and irrelevant
> here. Any other mismatch is a STOP condition.

## Git workflow

- Branch: `advisor/002-enforce-resource-ownership`
- Commit messages: short and imperative, e.g. `Scope customer contacts to the calling user`.
- Do NOT push.

## STOP conditions

- The navigation `t.Customer.Person.UserId` doesn't compile. That would mean the
  domain model differs from this plan's assumption (`Order.Customer`,
  `Customer.Person`, `Person.UserId`).
- A controller in `src/Client/Logiwall.Client.Core/Controllers` declares a
  method that calls the service directly, so changing a service signature
  breaks the client build. Report it; don't change client code.
- Any of the excerpts above don't match the live code.

## Commands you will need

| Purpose | Command (from `Logiwall-API/`) | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0, "Build succeeded" (≈40 pre-existing warnings are normal) |

## Tasks

### Task 1: Scope customer-contact read and update to the caller

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

### Task 2: Scope order options and option selection to the caller

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

### Task 3: Scope payment lookups to the caller (ownership only)

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
- [ ] this cycle's `tasks.md` status row updated
