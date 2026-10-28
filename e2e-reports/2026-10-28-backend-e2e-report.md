# Logiwall API — E2E report (front-driven), 2026-10-28

Result: **5 of 13 front calls work. 8 fail.** Of the 8 failures, 1 is a backend bug (B1),
6 are contract mismatches between front and API (C1–C6), and 1 (`Register`) fails only because
of C1. Endpoints that return 200 still have 5 more backend bugs (B2–B6).

Branch tested: `advisor/cycles-001-017` (working tree with uncommitted changes, as found).

## 1. How the test ran

```text
Chrome (DevTools MCP) ──drives──▶ Logiwall-Front (CRA, :3000, REACT_APP_BASE_URL=http://localhost:5030)
                                        │ calls
                                        ▼
                                 Logiwall.Server (:5030, Development)
                                   │ reads/writes              │ calls
                                   ▼                           ▼
                     throw-away SQL Server container     fake SMS / MyTipax / SEP server (:5999)
                     logiwall-e2e-sql (:14333),          (Node script; returns fixed, valid-shape
                     all migrations applied              responses and records the OTP)
```

* The API got its config from environment variables (CORS origin, provider credentials, SMS URL).
  No source file was changed.
* `AppSettings.MyTipaxBaseUrl`, `SepConnectUrl`, `SepPayBaseUrl` were pointed at the fake server.
* The UI flow was driven in the browser up to the map step of create-order. The map step needs an
  external Tipax geocoder that returns 401 (front issue F2). After that point, each endpoint was
  called from the same page with the **exact payload the front code builds**.
* Not verified: real MyTipax and SEP 724 responses. Amount findings (B2) depend on the real
  meaning of the MyTipax fields.

## 2. Endpoint results

| # | Front call | Status | Verdict |
|---|---|---|---|
| 1 | `POST /api/Identity/SendOtpCode` | 200 | OK |
| 2 | `POST /api/Identity/ConfirmationOtpCode` | 200 | Works, but contract mismatch C1 breaks the front |
| 3 | `POST /api/Person/Register` | 401 from front, 200 with a valid token | OK on the backend. Fails because of C1 |
| 4 | `POST /api/Identity/GetToken` | 200 / 401 on wrong password | OK |
| 5 | `GET /api/Person/GetUserProfile` | 200 | OK, but `email` is always null (B6) |
| 6 | `POST /api/Person/UpdateUserProfile` | 400 `UnknownException` | **Backend bug B1** |
| 7 | `POST /api/Person/ChangePassword` | 405 | **Endpoint does not exist** (C5) |
| 8 | `GET /api/Common/GetStaticDatas?Type=11/12` | 200 | OK |
| 9 | `POST /api/Order/CreateOrder` | 200 | OK, but no validation of `parcelTypeId` (B5) |
| 10 | `GET /api/Order/GetOptions` | 200 | Works; response shape does not match the front (C2); amounts look wrong (B2) |
| 11 | `POST /api/Order/SelectOption` | 400 with the front payload | Contract mismatch C3. 200 with `channel` + `callbackUrl` |
| 12 | `POST /api/Order/SelectPaymentType` | 405 | **Endpoint does not exist** (C4) |
| 13 | `POST /api/Order/GetOrders` | 200 | Works; response shape does not match the front (C6); data gaps (B3, B4) |

Security checks that passed: no token → 401 (`GetOrders`); wrong password → 401 with a generic
message; a foreign `orderId` → "order not found"; `UpdateUserProfile` rejects a foreign `userId`
(`PersonController.cs:78`); a callback URL outside `Payment:AllowedCallbackOrigins` → rejected.

## 3. Backend bugs (fix in the API)

### B1 — High: `UpdateUserProfile` always fails from the front
* Evidence: `Cannot insert the value NULL into column 'PhoneNumber', table 'People'`.
* Cause: `PersonService.UpdateUserProfileAsync` copies `requestDto.PhoneNumber` into
  `person.PhoneNumber` (`src/Logiwall.Infrastructure/Services/People/PersonService.cs`, the
  `person.PhoneNumber = requestDto.PhoneNumber;` line). The front does not send `phoneNumber`.
  The same method also resets `Kind` to 0 and `BirthDate` to null when the front does not send them.
* Also a security smell: a user can change their stored phone number through the body.
* Fix direction: do not take the phone number from the body (it is the login identity). Update
  only the fields the profile form owns.

### B2 — High (verify first): option amounts are mapped from the wrong field
* Evidence: provider returned `shippingCost: 1200000, specialService: 0, tax: 120000,
  finalAmount: 1320000`. API returned `baseAmount: 0, packageAmount: 0, taxAmount: 120000,
  totalAmount: 1320000`. The parts do not add up to the total.
* Cause: `OrderService.GetOptionsAsync` sets `ServiceAmount = SpecialService` and never reads
  `ShippingCost` (`OrderService.cs:150`).
* Verify against a real MyTipax inquiry response before fixing. Plan 010 / ADR-0001 already
  track the currency-unit question for the same amounts.

### B3 — Medium: `SelectOption` does not set `Order.FinalAmount`
* Evidence: after a successful `SelectOption`, `GetOrders` returns `finalAmount: 0` for that order.
* Cause: `SelectOptionAndPaymentAsync` copies service, packaging, insurance and tax amounts to the
  order, but not `FinalAmount` (`OrderService.cs`, block after "Record the accepted quote").

### B4 — Medium: `GetOrders` returns empty child lists
* Evidence: `orderParcels`, `orderInqueryOptions`, `orderPayments`, `orderStatuses` are `[]` for
  orders that have rows in those tables.
* Cause: `GetOrderListAsync` has no `Include` (or projection) for them. Either fill them or remove
  them from `OrderDto`, so the contract does not lie.

### B5 — Medium: no validation of `ParcelTypeId`
* Evidence: `CreateOrder` accepts `parcelTypeId: 1`. `GetOptions` then throws
  `NullReferenceException` at `OrderService.cs:117` (`Enum.GetName(...)!.ToUpper()`) and answers
  "provider service unavailable", which hides the real cause.
* Fix direction: validate `ParcelTypeId` (7 or 8) and `PackageTypeId` in `CreateOrderParcelRequestDto`.

### B6 — Low: e-mail from `Register` never shows in the profile
* `Register` saves the e-mail to `Customer.Email` (`PersonService.cs:49`). `GetUserProfile`
  reads Identity `User.Email` (`PersonService.cs:102`). Choose one place.

### B7 — Low: wrong success message on `ConfirmationOtpCode`
* A successful confirm returns `OtpCodeIsSent` ("the code was sent"), not a login-success message.

### B8 — Note: `SelectOption` returns 500 on a duplicate gateway reference code
* Seen only because the fake SEP returns the same `referenceCode`; real SEP returns unique codes.
  The `DbUpdateException` is not handled, so a gateway repeat would give a raw 500.

## 4. Contract mismatches (decide which side changes)

| ID | Endpoint | API sends / expects | Front sends / expects | Effect |
|---|---|---|---|---|
| C1 | `ConfirmationOtpCode` | `data.token` | `data.accessToken`, `data.expiresIn` | **Critical.** A new user cannot finish registration (`Bearer undefined` → 401). Existing users land on `/auth/register` |
| C2 | `GetOptions` | flat list `{id, providerName, delivereDate, baseAmount, packageAmount, taxAmount, totalAmount}` | grouped `{providerId, providerName, orderOptions[], pickupDates[{times[]}]}` | Option page cannot render; no pickup-time concept in the API |
| C3 | `SelectOption` | `{orderId, orderInqueryOptionId, channel*, callbackUrl*}`, returns the payment link | `{orderId, orderInqueryOptionId, pickupId}`, expects `pickupOptions[]` back | 400 validation error |
| C4 | `SelectPaymentType` | does not exist | `{paymentType, orderId, pickupOptionId, channel, callbackUrl}` → `paymentLink` | 405 |
| C5 | `Person/ChangePassword` | does not exist | change-password page | 405 |
| C6 | `GetOrders` | `data: OrderDto[]` | `data: {totalCount, result[]}` with `statusName`, `originCityName`, `destinationCityName` | Orders page is empty, pagination gets `NaN` |
| C7 | `Payment/VerifyPayment` | exists | **never called** | Paid orders stay `PaymentPending` forever |

The C-items show the front was built against a different API version. `OrderInqueryOptionsDto`
and `SelectOptionRequestDto` have not changed since the first commit (`git log`).
Recommended split: the front fixes C1 (rename two fields). The API team and the front team agree
on C2–C7 in one short contract session, because C2–C4 are one flow (choose option → pickup time →
payment) and the two sides model it differently.

## 5. Config needed in every environment

* `Cors:AllowedOrigins` is empty in `appsettings.json`. The front's origin must be set, or every
  browser call fails.
* `Payment:AllowedCallbackOrigins` must contain the front's callback origin. The front hard-codes
  `http://app.logiwall.ir`, which is rejected unless listed.
* SMS, MyTipax and SEP credentials (user-secrets / Key Vault).

## 6. Clean-up

* Container `logiwall-e2e-sql` (port 14333) and its data are test-only: `docker rm -f logiwall-e2e-sql`.
* The fake provider script is in the session scratchpad; nothing was added to the repository except
  this folder.
