# QA: `GET api/Dashboard/GetOverallAnalyticsStatsData`

The endpoint returns business-wide order counts for the Blazor admin panel (cycle 018,
ADR-0005). Only users with the `Admin` role can call it.

Response (`OverallAnalyticsStatsDataResponseDto`, camelCase JSON):

| Field | Meaning |
|---|---|
| `totalOrders` | All orders |
| `last30DaysOrders` | Orders with `CreationDate` in the last 30 days |
| `paidOrders` | Orders with at least one `OrderPayment` that has `Status = Success` |

## Acceptance criteria

1. An admin gets the counts of all orders, of all customers.
2. A signed-in user without the `Admin` role gets 403.
3. A request with no token gets 401.
4. The endpoint does not change data.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Read the counts as an admin. Add 4 orders for two customers: one 40 days old, one with a `Success` payment. Read again. | The counts grow by `4, 3, 1` |
| P2 | Sign in on the Blazor panel as an admin and open `/dashboard`. | The three cards show the same numbers as the database. |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | Call with no `Authorization` header. | 401 |
| N2 | Call with a customer token (OTP or password). | 403 |
| N3 | Call with an expired or changed token. | 401. The Blazor client signs the user out. |
| N4 | Sign in on the Blazor panel as a customer with a correct password. | "دسترسی به این منبع مجاز نیست." The panel stores no token. |

## Edge cases

* An order with two `Success` payments counts once in `paidOrders`.
* An order with only `Pending` or `UnSuccess` payments does not count in `paidOrders`.
* An order created exactly 30 days ago counts in `last30DaysOrders` (`>=`).
* A user who gets the `Admin` role must sign in again. The old token has no role.

## Authorization and resource ownership

* `[Authorize(Policy = "AdminsOnly")]` on `DashboardController` (`RequireRole("Admin")`).
* The Blazor sign-in check (`role: Admin` in the token) is only for the UI. The policy is the
  security boundary.
* The endpoint takes no user ID from the request.

## Validation

The endpoint has no input.

## Concurrency and idempotency

A GET with no side effects. Repeated calls return the same counts while the data does not change.

## Failure scenarios

* Database down: 500 with the standard error body from `ServerExceptionHandler`.
* Slow database: the request is cancelled when the client cancels (`CancellationToken` reaches
  the three `CountAsync` calls).

## Integration considerations

* Client: `IDashboardController.GetOverallAnalyticsStatsData` and `OverallStatsWidget`.
* The React app does not call this endpoint.
* `paidOrders` stays 0 until payment verification sets `PaymentStatus.Success` (cycle 012 Phase B).
* The `Admin` role comes from the `SeedAdminRole` migration. ADR-0005 has the operator SQL.

Automated tests: `tests/Logiwall.IntegrationTests/Orders/DashboardStatsTests.cs` (P1, N1, N2, and
the role claim in the admin token).
