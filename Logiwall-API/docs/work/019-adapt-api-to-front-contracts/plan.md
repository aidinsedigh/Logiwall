# Cycle 019 — plan

Read `spec.md` first. Branch: `advisor/cycles-001-017`.

### Task 1 — identity and profile (D1-D5)

`ConfirmationOtpResponseDto`, `IdentityController.ConfirmationOtpCode`, `AuthenticationService`,
`PersonService.UpdateUserProfileAsync`, `PersonController.Register`, new `ChangePassword`.
Tests: `ApiClient` reads `accessToken`; new profile and change-password tests.

### Task 2 — order quote, selection and payment (D6-D10, D14)

`OrderService.GetOptionsAsync` groups by provider and adds pickup slots (`PickupSlots`).
`SelectOptionAsync` records the choice. `SelectPaymentTypeAsync` creates the link.
`CreateOrderParcelRequestDto` validation. Tests: update the quote/payment flow tests; unit test
for `PickupSlots`.

### Task 3 — order list (D11-D13)

`GetOrderListAsync` returns `OrderListResponseDto(TotalCount, Result)` with names and the chosen
amount. Tests: `OrderListTests`, `OrderQuoteFlowTests.Order_list_shows_the_order_state`.

### Task 4 — QA files, build, tests

QA files for the changed endpoints. `dotnet build Logiwall.Server.slnf`, unit and integration tests.
