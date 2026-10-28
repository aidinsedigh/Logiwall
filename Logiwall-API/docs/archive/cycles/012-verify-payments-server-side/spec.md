# Cycle 012 — A payment counts as successful only after the server verifies it with the gateway against the amount the server expects: spec

## Contents

- L19 Summary
- L28 Why this matters
- L48 Repo facts you need
- L72 Current state
- L112 Scope
- L134 Maintenance notes

Status: BLOCKED (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 012 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P1 (critical: payments are currently trusted from the client)
- **Effort**: L
- **Risk**: MED–HIGH (money; external gateway contract)
- **Depends on**: docs/archive/cycles/002-enforce-resource-ownership/, docs/archive/cycles/005-establish-test-and-ci-baseline/, docs/archive/cycles/010-investigate-currency-units/, docs/archive/cycles/011-make-order-quote-flow-idempotent-and-stateful/
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

## Maintenance notes

- Pending payments that are never verified (the user closes the browser) need a
  reconciliation job: periodically verify `Pending` rows older than N minutes. That is a follow-up.
- Operator: set `Payment__AllowedCallbackOrigins__0=<frontend origin>` in production.
- The frontend must call `POST api/Payment/VerifyPayment {orderId}` when the gateway
  redirects back. Coordinate with the frontend team, who currently expect `/api/Order/SelectPaymentType`.
- Reviewers: check that nothing other than `VerifyWithGatewayAsync` ever assigns `PaymentStatus.Success`.
