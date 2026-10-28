# QA: `POST api/Order/SelectPaymentType`

New in cycle 019 (E2E C4, B8). The endpoint creates the SEP 724 payment link for the option
that `SelectOption` recorded. `GetOptions.QA.md` has the flow diagram.

Request (the front's `IgoPayPayload`, `Logiwall-Front/src/features/create-order/api/pay.ts`):
`{ paymentType, orderId, pickupOptionId, channel, callbackUrl }`.

Response `data`: `{ paymentLink, referenceCode, requestId }`. The front redirects to `paymentLink`.

## Acceptance criteria

1. `paymentType` 71 (`PaymentMethod.Online`) with the chosen option returns 200 and a `paymentLink`.
2. The server sends the chosen option's `FinalAmount` to the gateway. The client cannot set the amount.
3. The service saves one `OrderPayment` (`Pending`, with the gateway reference) and sets the order to `PaymentPending`.
4. If `channel` is null or empty, the service sends `pwa`.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Quote, select, then pay with 71 and an allowed `callbackUrl`. | 200 `paymentLink`, payment `Pending` with amount = option total |
| P2 | Pay twice for the same order. | Two `Pending` payments. Verification checks each one. |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | `paymentType` 72 (cash on delivery) | 400 `PaymentTypeNotSupported`. The front does not call the API for 72. |
| N2 | `pickupOptionId` of an option that is not chosen | 400 `InvalidOrderOption` |
| N3 | Pay before `SelectOption` | 400 `InvalidOrderOption` |
| N4 | `callbackUrl` outside `Payment:AllowedCallbackOrigins` | 400 `InvalidCallbackUrl`. No gateway call. |
| N5 | Another user's order | 400 `InvalidOrder` |
| N6 | A paid order | 400 `OrderIsAlreadyPaid` |
| N7 | A user who has not registered | 400 `UserIsNotRegistered` |

## Edge cases

* The gateway returns a reference code that is already saved: 400 `PaymentLinkFailed`, not 500. The service logs the `DbUpdateException`.
* The front hard-codes `callbackUrl = http://app.logiwall.ir/app/home`. Add that origin to `Payment:AllowedCallbackOrigins` in every environment.

## Authorization and resource ownership

Order and option must belong to the token's customer.

## Validation

`callbackUrl` `[Required]` and absolute. `paymentType` must be `Online`.

## Concurrency and idempotency

Not idempotent: each call asks the gateway for a new link.

## Failure scenarios

Gateway gives no link: 400 `PaymentLinkFailed`. The provider text goes to the log only (ADR-0003).

## Integration considerations

* Verification is blocked: `VerifyPayment` is never called by the front (E2E C7), and its gateway check is a stub (plan 012 Phase B). Paid orders stay `PaymentPending`.
* The amount unit is not confirmed (ADR-0001).

Automated tests: `PaymentFlowTests`.
