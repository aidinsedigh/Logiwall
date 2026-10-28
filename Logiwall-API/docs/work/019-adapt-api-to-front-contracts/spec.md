# Cycle 019 — adapt the API to the front's contracts

Source: `../e2e-reports/2026-10-28-backend-e2e-report.md` (git root). Rule from the user: the
front (`Logiwall-Front`) does not change. The API changes its contracts to match the front.

## Decisions

| ID | Report item | Decision |
|---|---|---|
| D1 | C1 | `ConfirmationOtpCode` returns `data.accessToken` and `data.expiresIn` (seconds), like `GetToken`. `token` is removed. |
| D2 | B7 | A successful confirm returns the new key `OtpLoginSucceeded`. |
| D3 | B1 | `UpdateUserProfile` never writes `PhoneNumber`. It keeps `Kind` and `BirthDate` when the body does not send them. |
| D4 | B6 | `Register` also writes the e-mail to Identity `User.Email`, the field `GetUserProfile` reads. |
| D5 | C5 | New `POST api/Person/ChangePassword {oldPassword, newPassword}` → `ServiceResponse<bool>`. |
| D6 | C2 | `GetOptions` returns one item for each provider: `{providerId, providerName, orderOptions[], pickupDates[{id, description, times[{id, description}]}]}`. |
| D7 | B2 | `ServiceAmount` = MyTipax `shippingCost` + `specialService`. A missing amount counts as 0. Not verified against real MyTipax (ADR-0001). |
| D8 | C3 | `SelectOption {orderId, orderInqueryOptionId, pickupId}` records the choice and `Order.PickupDateTime`. It returns `{pickupOptions[]}`. It no longer creates the payment. |
| D9 | C4 | New `POST api/Order/SelectPaymentType {paymentType, orderId, pickupOptionId, channel?, callbackUrl}` creates the payment link. `paymentType` must be 71 (online). `pickupOptionId` is the chosen `OrderInqueryOption.Id`. `channel` defaults to `pwa`. |
| D10 | B8 | A `DbUpdateException` when the payment is saved returns `PaymentLinkFailed`, not HTTP 500. |
| D11 | B3 | `GetOrders` reads `finalAmount` from the chosen option. `Order.FinalAmount` has no column, so no migration. |
| D12 | C6 | `GetOrders` returns `{totalCount, result[]}`. Each order has `statusName`, `originCityName`, `destinationCityName`. |
| D13 | B4 | The four empty child lists leave `OrderDto`. The front does not read them. |
| D14 | B5 | `ParcelTypeId` must be 7 or 8, `PackageTypeId` 0 or 1, and an order has at least one parcel. |

## Pickup slots (D6, D8)

The API has no pickup source. MyTipax only quotes (`docs/archive/audit-2026-10-27.md`, item 2).
Shortcut: fixed slots. Three days from tomorrow (Tehran time, UTC+03:30), windows 9-12, 12-15,
15-18. Date id `yyyy-MM-dd`, time id `yyyy-MM-ddTHH`. `SelectOption` accepts only an offered
time id. Replace this when the courier booking API is integrated.

## Open questions (the user must decide)

| ID | Question | Current behavior |
|---|---|---|
| Q1 | Which pickup days and windows does Logiwall offer? Must a courier API supply them? | Fixed slots in `PickupSlots` (see above). No courier booking. |
| Q2 | Which gender numbers are correct? The front sends 1 (male) and 3 (female). `Constants.Gender` has Male = 3, Female = 4. | The numbers round-trip, so the UI is correct. The server reads 3 as Male. |
| Q3 | What do MyTipax `shippingCost` and `specialService` mean? (D7, ADR-0001, plan 010) | `baseAmount` = `shippingCost` + `specialService`. Checked only against the E2E fake server. |
| Q4 | Where does the pickup price come from? | `pickupAmount` is 0. |
| Q5 | Does the front need the order summary in the `SelectOption` response? | Only `pickupOptions` is sent. The front reads only `pickupOptions[0].id`. |

## Not fixed

| ID | Item | Why, or what to do |
|---|---|---|
| N1 | C7: paid orders stay `PaymentPending`. | Blocked by plan 012 Phase B: `Sep724Provider.GetPaymentInfoAsync` is not implemented, and the front never calls `VerifyPayment`. |
| N2 | `UpdateUserProfile` with a national code that another person has returns `UnknownException`. | The unique index rejects it. Add the `NationalCodeIsTaken` check that `CreatePersonAsync` has. |
| N3 | `Register` returns `succuss: true` with HTTP 400 when the password reset fails. | `PersonController.Register` does not set `Succuss = false` on that path. |
| N4 | Config in every environment: `Payment:AllowedCallbackOrigins` must have `http://app.logiwall.ir`, and `Cors:AllowedOrigins` must have the front's origin. | The front hard-codes that callback URL. |
| N5 | Local `appsettings.json` changes (SQL `sa` password, `AdminSeed`) are not committed. | Secrets (§14.1). Move them to user-secrets. |
| N6 | The React app was not run against the new API. | Only the integration tests used the front's bodies. Run the E2E flow again. |
