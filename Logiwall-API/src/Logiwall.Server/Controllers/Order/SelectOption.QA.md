# QA: `POST api/Order/SelectOption`

The endpoint records the option and the pickup time the customer chose. Cycle 019 (E2E C3)
changed the body to the front's body, and moved the payment link to `SelectPaymentType`.
`GetOptions.QA.md` has the flow diagram.

Request: `{ orderId, orderInqueryOptionId, pickupId }`. `pickupId` is a `pickupDates[].times[].id` from `GetOptions`.

Response `data`: `{ pickupOptions: [{ id, providerPickupOptionId, finalAmount, basePrice, itemPrice, tax, description }] }`.
`id` is the chosen `OrderInqueryOption.Id`. The front sends it back as `pickupOptionId`.

## Acceptance criteria

1. The front body returns 200 with one `pickupOptions` item.
2. The chosen option gets `Choosen = true`. The other options of the order get `false`.
3. The order gets the option's amounts, `PickupDateTime` (the slot start, UTC+03:30) and state `SelectedServiceProvider`.
4. No payment row is created.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Quote, then select option A with the first offered time. | 200, `pickupOptions[0].id` = A |
| P2 | Select option B after A. | 200. Only B is `Choosen`. |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | An option of another order | 400 `InvalidOrderOption` |
| N2 | Another user's order | 400 `InvalidOrder` |
| N3 | A `pickupId` that is not offered now (past day, Friday, not a window start, text) | 400 `InvalidPickupTime` |
| N4 | No `pickupId` | 400 validation error |
| N5 | A paid order | 400 `OrderIsAlreadyPaid` |

## Edge cases

* A `pickupId` from a page that was open over midnight can become "not offered". The customer must quote again.

## Authorization and resource ownership

Order and option must belong to the token's customer.

## Validation

`SelectOptionRequestDto.PickupId` `[Required]`. `PickupSlots.Find` accepts only an offered time.

## Concurrency and idempotency

The same body twice gives the same state.

## Failure scenarios

Database error: 500 with the standard error body.

## Integration considerations

* Pickup times are fixed slots (`PickupSlots`, shortcut). No courier booking happens.
* The front type `IselectOption` lists more fields. The front reads only `pickupOptions[0].id`, so the API sends only `pickupOptions`.

Automated tests: `OrderQuoteFlowTests.SelectOption_*`, `OrderOwnershipTests.Option_of_another_order_cannot_be_selected`.
