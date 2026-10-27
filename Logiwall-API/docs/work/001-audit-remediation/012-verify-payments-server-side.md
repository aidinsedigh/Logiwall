# Plan 012: A payment counts as successful only after the server verifies it with the gateway against the amount the server expects

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems Logiwall-API/src/Logiwall.Infrastructure/Providers/Sep724Provider.cs Logiwall-API/src/Logiwall.Server/Controllers/Order Logiwall-API/src/Logiwall.Domain/Entities/OrderItems/OrderPayment.cs Logiwall-API/src/Logiwall.Shared/Enums/PaymentStatus.cs Logiwall-API/src/Logiwall.Application/Abstractions/OrderItems`
> Expected from earlier plans: 002 (ownership in `PaymentService`), 003
> (`Sep724Options`), 009 (`ILogger` and no `ex.Message` in `PaymentService`), and
> 011 (`OrderState` values 3–4, option marked `Choosen`, order state set in
> `SelectOptionAndPaymentAsync`, unused `Func` removed from `PaymentService`).
> **Read `docs/adr/0001-currency-units.md` (plan 010) first.** If it
> doesn't exist, STOP.

## Status

- **Priority**: P1 (critical: payments are currently trusted from the client)
- **Effort**: L
- **Risk**: MED–HIGH (money; external gateway contract)
- **Depends on**: docs/work/001-audit-remediation/002-enforce-resource-ownership.md, docs/work/001-audit-remediation/005-establish-test-and-ci-baseline.md, docs/work/001-audit-remediation/010-investigate-currency-units.md, docs/work/001-audit-remediation/011-make-order-quote-flow-idempotent-and-stateful.md
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Today `POST api/Payment/UpdatePaymentOrderStatus` lets any logged-in user record
a **successful** payment with any amount. The service hardcodes
`Status = PaymentStatus.Success` (`//todo Send Payment Request to Gateway`) and
stores the client-supplied `Amount`, `PayTrackingCode` and `PayDate`. The
gateway's only verification method, `Sep724Provider.GetPaymentInfoAsync`,
throws `NotImplementedException`. Nothing links the money actually charged to
the order. The callback URL the gateway redirects to also comes from the client,
unchecked.

This plan has two phases:
- **Phase A** (no gateway documentation needed) makes the server the only source
  of truth. It records a *pending* payment with the server-computed amount when
  the payment link is issued, removes client-reported status, and allow-lists
  the callback URL. After Phase A, **nothing can mark a payment successful**,
  which is strictly safer than today.
- **Phase B** (needs the SEP 724 API documentation) implements real
  verification and the `Paid` transition.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. Paths are relative to `Logiwall-API/`.
- .NET 8, EF Core 8 SQL Server. Migrations are in `src/Logiwall.Infrastructure/Data/Migrations`.
  EF commands run from `src/Logiwall.Server`:
  `dotnet ef migrations add <Name> --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations`.
- Entity configurations live in `src/Logiwall.Infrastructure/Data/Configurations/**` and are
  applied by `ApplyConfigurationsFromAssembly`. `OrderPaymentConfiguration.cs` is currently empty.
- `IPaymentController` in `src/Client/Logiwall.Client.Core/Controllers/Order/IPaymentController.cs`
  declares **no** methods, and the React frontend does not call the Payment controller.
  The server action can be replaced freely.
- The React frontend expects `POST /api/Order/SelectPaymentType`, which does not exist.
  **That stays out of scope.** Mention it in your report.
- `ServiceResponse<T>`/`Succuss` is a misspelled wire contract. Enums are stored as ints/shorts,
  so **append values only**.
- SEP types (`src/Logiwall.Shared/Dtos/Providers/Sep724/`):
  - `GetPaymentTokenResponseDto { long RequestId; Guid ReferenceCode; string? PaymentLink }`
  - `GetPaymentInfoRequestDto { long RequestId; Guid ReferenceCode }` (implements `IHttpRequest`)
  - `GetPaymentInfoResponseDto { long RequestId; string? MPin; string? PaymentParam; Guid ReferenceCode; long Amount }`
  - `SepResponseBase<T> { string? Message; T? Data; string? Type; string? Title; int Status; string? Detail; int Code }`
  Note that `GetPaymentInfoResponseDto` has **no explicit success field**. Phase B must find out
  from the documentation how success is expressed.
- Test harness from plan 005: `tests/Logiwall.IntegrationTests` and `tests/Logiwall.UnitTests`.

## Current state

`src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs` (`PaymentOrderStatusAsync`, after plans 002/009):
loads the caller's order, then builds `result` with `Status = PaymentStatus.Success`
unconditionally, finds or creates an `OrderPayment` by `PayTrackingCode` (scoped to
the order), sets `Status = Success`, and saves. On a new row it uses `requestDto.Map()`
(Mapperly, `src/Logiwall.Infrastructure/Mapper/PaymentMapper.cs`), which copies the client's `Amount`, `MerchantId`,
`TerminalId`, `PayTrackingCode` and `PayDate`.

`src/Logiwall.Server/Controllers/Order/PaymentController.cs`:
```csharp
[HttpPost]
public async Task<ActionResult> UpdatePaymentOrderStatus(PaymentRequestDto requestDto, CancellationToken cancellationToken)
{
    var userId = User.GetUserId();
    var result = await paymentService.PaymentOrderStatusAsync(requestDto, userId);
    ...
```

`src/Logiwall.Domain/Entities/OrderItems/OrderPayment.cs`: `Id`, `Amount` (decimal), `MerchantId?`, `TerminalId?`,
`PayTrackingCode` (`[Required, StringLength(25)]`), `PaymentType`, `Status` (`PaymentStatus`), `PayDate`, `CreationDate`, `OrderId`.

`src/Logiwall.Shared/Enums/PaymentStatus.cs`: `UnSuccess = 0, Success = 1`.

`src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`, `SelectOptionAndPaymentAsync`. After plan 011 it marks
the option chosen, sets the state to `SelectedServiceProvider`, then calls
`sep724Provider.GetPaymentTokenAsync(new PaymentTokenRequestDto { Amount = orderInquiry.FinalAmount, PhoneNumber, Channel, CallbackUrl = requestDto.CallbackUrl, OrderNumber })`,
and returns `ReferenceCode`, `RequestId` and `PaymentLink`. **No payment row is written.**

`src/Logiwall.Infrastructure/Providers/Sep724Provider.cs`:
```csharp
public Task<SepResponseBase<GetPaymentInfoResponseDto>> GetPaymentInfoAsync(GetPaymentInfoRequestDto requestDto)
{
    throw new NotImplementedException();
}
```
The token call posts to `appSettings.SepPayBaseUrl` + `"token"` with a bearer token
from `GetTokenAsync()`. Copy that pattern (headers including `x-trace-id`, `HttpProviderRequest`) for Phase B.
Its 401 retry uses an instance counter, `tryToGetToken`, that is never reset.

## Commands you will need

| Purpose | Command | Expected |
|---|---|---|
| Build | `dotnet build Logiwall.Server.slnf` | exit 0 |
| Add migration | (see Repo facts) name `AddGatewayFieldsToOrderPayment` | "Done." |
| Script migration | `cd src/Logiwall.Server && dotnet ef migrations script --idempotent --project ../Logiwall.Infrastructure --startup-project . -o ../../artifacts/payment.sql` | file written. **Do not commit `artifacts/`.** |
| Tests | `dotnet test tests/Logiwall.UnitTests` and `dotnet test tests/Logiwall.IntegrationTests` | all pass |

## Scope

**In scope**:
- `src/Logiwall.Shared/Enums/PaymentStatus.cs` (append `Pending = 2`)
- `src/Logiwall.Domain/Entities/OrderItems/OrderPayment.cs` (add gateway fields)
- `src/Logiwall.Infrastructure/Data/Configurations/OrderItems/OrderPaymentConfiguration.cs`
- `src/Logiwall.Infrastructure/Data/Migrations/<new>_AddGatewayFieldsToOrderPayment*.cs` and `AppDbContextModelSnapshot.cs` (generated)
- `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs` (`SelectOptionAndPaymentAsync` only)
- `src/Logiwall.Infrastructure/Services/OrderItems/PaymentService.cs` (rewrite)
- `src/Logiwall.Application/Abstractions/OrderItems/IPaymentService.cs`
- `src/Logiwall.Shared/Dtos/PaymentItems/VerifyPaymentRequestDto.cs` (create)
- `src/Logiwall.Server/Controllers/Order/PaymentController.cs`
- `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs` (Phase B, plus the amount conversion if plan 010's verdict is (b))
- `src/Logiwall.Infrastructure/Options/PaymentOptions.cs` (create), `src/Logiwall.Infrastructure/Services/DependencyInjection.cs`, `src/Logiwall.Server/appsettings.json` (the `Payment` section)
- tests: `tests/Logiwall.UnitTests/Providers/Sep724ProviderTests.cs`, `tests/Logiwall.IntegrationTests/Payments/PaymentFlowTests.cs` (create), and fakes as needed

**Out of scope**:
- `SelectPaymentType` (the frontend contract), refunds, cancellation, wallets (`PaymentType.Wallet`).
- Shipment booking after payment.
- Changing how the SEP payment token is requested, beyond the amount conversion that plan 010 mandates.
- `PaymentRequestDto` and `PaymentMapper`. Leave them; they become unused, and plan 015 deletes them.

## Git workflow

- Branch `advisor/012-verify-payments`. Commit per step. Do NOT push.

## Steps — Phase A (server-side truth; no gateway docs needed)

### Step A1: Model

1. Append `[Description("در انتظار")] Pending = 2` to `PaymentStatus`.
2. In `OrderPayment`, add:
   ```csharp
   public long? GatewayRequestId { get; set; }
   public Guid? GatewayReferenceCode { get; set; }
   public long? VerifiedAmount { get; set; }
   public DateTimeOffset? VerifiedAt { get; set; }
   ```
3. In `OrderPaymentConfiguration.Configure`, add
   `builder.HasIndex(p => p.GatewayReferenceCode).IsUnique().HasFilter("[GatewayReferenceCode] IS NOT NULL");`.
4. Add migration `AddGatewayFieldsToOrderPayment`.

**Verify**: build → exit 0. The generated migration contains only `AddColumn` ×4 and `CreateIndex` ×1 on `OrderPayments`. If it contains anything else, STOP.

### Step A2: Record a pending payment when the link is issued

1. Create `PaymentOptions { public const string SectionName = "Payment"; public string[] AllowedCallbackOrigins { get; set; } = []; }`
   in `src/Logiwall.Infrastructure/Options/`, bind it in `AddInfrastructure`, and add
   `"Payment": { "AllowedCallbackOrigins": [] }` to `appsettings.json`.
2. In `OrderService.SelectOptionAndPaymentAsync` (inject `IOptions<PaymentOptions>`):
   - **Before** calling the gateway: parse `requestDto.CallbackUrl` with `Uri.TryCreate(..., UriKind.Absolute, ...)`.
     If the configured list is non-empty and `uri.GetLeftPart(UriPartial.Authority)` is not
     in it (case-insensitive), return `Succuss = false, Message = "Invalid callback URL"`.
     If the list is empty, allow it and log a warning (development).
   - Compute the amount sent to the gateway from `orderInquiry.FinalAmount`, applying plan 010's
     verdict: (a) unchanged; (b) the conversion the record specifies; (c) **unchanged, plus a
     `// TODO(plan 010): unit unconfirmed` comment**. Call it `expectedGatewayAmount` (`long`, rounded
     with `decimal.Round(..., MidpointRounding.AwayFromZero)`, not truncated) and pass it as `Amount`.
   - **After** a successful token response: in one `SaveChangesAsync`, add
     `new OrderPayment { OrderId = order.Id, Amount = expectedGatewayAmount, PaymentType = PaymentType.BankGateway, Status = PaymentStatus.Pending, PayTrackingCode = data.RequestId.ToString(CultureInfo.InvariantCulture), PayDate = DateTimeOffset.Now, GatewayRequestId = data.RequestId, GatewayReferenceCode = data.ReferenceCode }`,
     set `order.State = (short)OrderState.PaymentPending`, and add an `OrderStatus` history row (`Description = "Payment link issued"`), following plan 011's pattern.

**Verify**: build → exit 0.

### Step A3: Remove client-reported payment status

1. Create `src/Logiwall.Shared/Dtos/PaymentItems/VerifyPaymentRequestDto.cs`: `public class VerifyPaymentRequestDto { [Required] public int OrderId { get; set; } }`.
2. Replace `IPaymentService` with:
   ```csharp
   Task<ServiceResponse<PaymentResponseDto>> VerifyPaymentAsync(VerifyPaymentRequestDto requestDto, int userId, CancellationToken cancellationToken);
   ```
3. Rewrite `PaymentService`:
   - load the caller's order (keep plan 002's ownership predicate); if it is null → `Succuss=false`, `AppStrings.InvalidOrder` (as today);
   - load the **latest** `OrderPayment` for the order where `GatewayReferenceCode != null`, ordered by `Id` descending; if there is none → `Succuss=false, Message="No payment to verify"`;
   - if it is already `Success` or `UnSuccess` and `VerifiedAt != null`, return the stored result (**idempotent**, no gateway call);
   - otherwise call `VerifyWithGatewayAsync(payment, cancellationToken)` (a private method).
     **In Phase A this method returns `null`** (meaning "verification unavailable"), with the comment
     `// Phase B: implement with Sep724Provider.GetPaymentInfoAsync`;
   - if the result is null → return `Succuss=false, Message="Payment verification is not available yet"`, leaving the payment `Pending`.
4. In `PaymentController`, replace the action with
   `[HttpPost] public async Task<ActionResult> VerifyPayment(VerifyPaymentRequestDto requestDto, CancellationToken cancellationToken)`,
   which calls `VerifyPaymentAsync(requestDto, User.GetUserId(), cancellationToken)` and keeps the BadRequest/Ok shape.

**Verify**: build → exit 0; `grep -rn "PaymentStatus.Success" src/Logiwall.Infrastructure/Services` → no output (in Phase A nothing assigns Success).

### Step A4: Phase A tests

`tests/Logiwall.IntegrationTests/Payments/PaymentFlowTests.cs`. Replace `ISep724Provider` in the
factory with a `FakeSep724Provider` that returns `Data = new GetPaymentTokenResponseDto { RequestId = <random>, ReferenceCode = Guid.NewGuid(), PaymentLink = "https://pay.test/x" }`
and records the requested amount.
1. `SelectOption_creates_pending_payment_with_server_amount`: quote (via plan 011's fake postal provider), then select. There is exactly one `OrderPayment` with `Status = Pending`, an `Amount` equal to the expected gateway amount, and a non-null `GatewayReferenceCode`. `order.State == 3`.
2. `Callback_url_outside_allow_list_is_rejected`: a factory variant with `Payment:AllowedCallbackOrigins:0 = https://app.test`, and callback `https://evil.test/cb` → 400, the fake gateway is not called, and there is no payment row.
3. `Verify_never_marks_success_in_phase_A`: after test 1, `POST api/Payment/VerifyPayment {orderId}` → 400, and the payment is still `Pending`.
4. `Old_client_status_endpoint_is_gone`: `POST api/Payment/UpdatePaymentOrderStatus` → 404.
5. `Other_user_cannot_verify`: user B calls VerifyPayment on A's order → 400.

**Verify**: all tests pass. **Commit Phase A now.** Phase A can be shipped on its own.

## Steps — Phase B (requires SEP 724 documentation)

### Step B0: Gate

You need, **from the operator or a document in the repo**: (1) the SEP 724 endpoint path
and method for looking up or verifying a payment by `requestId`/`referenceCode`; (2) how
the response indicates success; (3) whether the amount is in rials. If you don't have
these, **stop here**. Mark the plan `BLOCKED (Phase A done; Phase B needs SEP verify API docs)`
in `docs/work/001-audit-remediation/tasks.md`. Do not guess endpoint paths.

### Step B1: Implement `GetPaymentInfoAsync`

Follow `GetPaymentTokenAsync`'s pattern: call `GetTokenAsync()`, use the same header list
with a new `x-trace-id`, and call `httpProvider.PostAsync`/`GetAsync` (as the docs specify)
with `BaseAddress = appSettings.SepPayBaseUrl` and the documented path. Log the
`x-trace-id` value (not the headers) at Information level, so payments can be matched up with SEP support.

### Step B2: Implement `VerifyWithGatewayAsync`

- Call `GetPaymentInfoAsync(new() { RequestId = payment.GatewayRequestId!.Value, ReferenceCode = payment.GatewayReferenceCode!.Value })`.
- Success requires **all** of: the documented success indicator is true;
  `response.Data.ReferenceCode == payment.GatewayReferenceCode`; and
  `response.Data.Amount == (long)payment.Amount`.
- In one transaction: set `Status`, `VerifiedAmount = response.Data.Amount`, `VerifiedAt = now`; set the order state to
  `Paid` (on success) or `PaymentFailed`; add an `OrderStatus` row. On an amount mismatch, log at **Error** level with the order ID and both amounts.

### Step B3: Phase B tests

Unit tests in `tests/Logiwall.UnitTests/Providers/Sep724ProviderTests.cs`, using a hand-written `FakeHttpProvider : IHttpProvider`
that captures the body and returns a configured response:
1. The split amounts sum to the total, and the Logiwall share matches `LogiwallShare` percent with the rounding the decision record specifies.
2. `GetPaymentInfoAsync` calls the documented path with `requestId` and `referenceCode`.

Integration tests (extend `PaymentFlowTests`, with the fake gateway returning a configurable info response):
3. Matching amount and success → payment `Success`, order `Paid` (state 2).
4. Amount 1 lower → payment `UnSuccess`, order `PaymentFailed` (state 4).
5. Verify called twice → the gateway info is called once (idempotent).

## Done criteria

- [ ] Phase A: build exits 0; tests A4 (5) pass; `grep -rn "UpdatePaymentOrderStatus" src` → no output; the migration contains only the 4 columns + 1 index
- [ ] Phase B (or the documented BLOCKED status): tests B3 pass; `grep -n "NotImplementedException" src/Logiwall.Infrastructure/Providers/Sep724Provider.cs` → no output
- [ ] `artifacts/` is not committed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated (`DONE`, or `BLOCKED` with the Phase B reason)

## STOP conditions

- `docs/adr/0001-currency-units.md` is missing.
- The verdict in the record is (b), but it doesn't specify the conversion precisely.
- The generated migration touches anything other than `OrderPayments`.
- Phase B: the documentation shows that verification must happen from the gateway's
  **server-to-server callback** rather than a lookup. That needs a new anonymous endpoint
  with signature validation. Stop and report, so that endpoint can be designed with the
  gateway's authenticity mechanism.

## Maintenance notes

- Pending payments that are never verified (the user closes the browser) need a
  reconciliation job: periodically verify `Pending` rows older than N minutes. That is a follow-up.
- Operator: set `Payment__AllowedCallbackOrigins__0=<frontend origin>` in production.
- The frontend must call `POST api/Payment/VerifyPayment {orderId}` when the gateway
  redirects back. Coordinate with the frontend team, who currently expect `/api/Order/SelectPaymentType`.
- Reviewers: check that nothing other than `VerifyWithGatewayAsync` ever assigns `PaymentStatus.Success`.
