# QA: `POST api/Order/CreateOrder`

The endpoint creates an order for the signed-in customer. Cycle 019 (E2E B5) added parcel
validation, so a bad parcel type fails here and not later in `GetOptions`.

## Acceptance criteria

1. The front body (`Logiwall-Front/src/types/create-order.ts`, `IcreateOrder`) returns 200 with `orderId` and `orderNumber`.
2. `parcelTypeId` must be 7 (envelope) or 8 (package). Other values return 400.
3. `packageTypeId` must be 0 (no packing) or 1 (packing). Other values return 400.
4. An order must have at least one parcel.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Send the front body with one envelope. | 200, `orderNumber` like `L000123` |
| P2 | Call `GetOptions` for the new order. | 200 with options |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | `parcelTypeId: 1` | 400 validation error |
| N2 | `packageTypeId: 2` | 400 validation error |
| N3 | `orderParcels: []` or no `orderParcels` | 400 validation error |
| N4 | No token | 401 |
| N5 | A user who has not registered | 400 `CustomerNotFound` |

## Edge cases

* Orders that were saved before cycle 019 with a bad parcel type still fail in `GetOptions` with `ProviderServiceUnavailable`.

## Authorization and resource ownership

The customer comes from the token. The body has no customer field.

## Validation

`CreateOrderRequestDto` length limits, `CreateOrderParcelRequestDto` `[EnumDataType]` and `[Range(0, 1)]`.

## Concurrency and idempotency

Each call creates a new order. The front calls it once per wizard.

## Failure scenarios

Database error: 400 `UnknownException`, and the service logs the exception.

## Integration considerations

The front keeps `orderId` in the query string for the next steps.

Automated tests: `ProfileTests.CreateOrder_rejects_an_unknown_parcel_type`.
