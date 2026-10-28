# QA: `POST api/Order/GetOrders`

The endpoint returns a page of the signed-in customer's orders. Cycle 019 (E2E C6, B3, B4)
changed the response to the front's `IorderResponse` (`Logiwall-Front/src/types/orders.ts`).

Request: `{ hasPagination, pageNumber, pageSize, filterGroups? }`.

Response `data`: `{ totalCount, result[] }`. Each order has the `OrderDto` fields and also:

| Field | Source |
|---|---|
| `statusName` | `OrderState` `[Description]`, for example "در انتظار پرداخت" |
| `originCityName`, `destinationCityName` | `StaticDatas.Title` for `OriginCity`, `DestinationCity` |
| `finalAmount` | The chosen `OrderInqueryOption.FinalAmount`. 0 before `SelectOption`. |

`orderParcels`, `orderInqueryOptions`, `orderPayments` and `orderStatuses` are no longer in the response.

## Acceptance criteria

1. `totalCount` is the number of matching orders before paging. The front computes the page count from it.
2. `result` is the requested page, newest first when no sort is sent.
3. Each order has `statusName` and both city names.
4. After `SelectOption`, `finalAmount` is the chosen option's total.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Create 3 orders. Ask for page 1, size 2. | `totalCount` 3, 2 items |
| P2 | Filter `originCity = 7`. | Only orders from city 7; `totalCount` counts only them |
| P3 | Select an option, then list. | `finalAmount` = the option total |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | No token | 401 |
| N2 | A user who has not registered | 400 `CustomerNotFound` |
| N3 | A filter on a property that is not in the allow list | 200. The service skips that condition (`QueryableExtensions.ApplyFilters`). |

## Edge cases

* A city ID with no `StaticDatas` row: the name is null.
* `hasPagination = false`: at most 100 orders, but `totalCount` is the full count.

## Authorization and resource ownership

The list has only the token's customer's orders.

## Validation

Filters and sorts accept only `OrderFilterableProperties`.

## Concurrency and idempotency

Read only.

## Failure scenarios

Database down: 500 with the standard error body.

## Integration considerations

The front asks for `pageSize: 3` and computes `Math.ceil(totalCount / 3)` pages.

Automated tests: `OrderListTests`, `OrderQuoteFlowTests.Order_list_shows_the_order_state`.
