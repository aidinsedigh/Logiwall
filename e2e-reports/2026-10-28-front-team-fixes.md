# Logiwall-Front — fixes found in the E2E test, 2026-10-28

The API team ran the front (`npm start`) against the local API and drove it in Chrome.
This file lists what the **front** must change. Items marked "agree first" need a short
contract talk with the API team, because the API may change instead.

> **Update (API cycle 019):** the API now matches the front's contracts. Do not change the front
> for F1, F3, F4, F6, F7 or F8. In F5, the callback-URL part is API config only
> (`Payment:AllowedCallbackOrigins`). The verification part is still open: API plan 012 Phase B
> blocks it. F2, F9 and F10 are still front work. See
> `Logiwall-API/docs/work/019-adapt-api-to-front-contracts/spec.md`.

Severity: **Critical** = a main flow is blocked. **High** = a feature is broken.
**Medium** = wrong data or risk. **Low** = cleanup.

---

## F1 — Critical: OTP login does not save the token

* File: `src/features/auth/api/otp.ts` (`HandleConfirmOtpCode`).
* The API answers `ConfirmationOtpCode` with:
  ```json
  { "data": { "fullName": null, "token": "<jwt>" }, "succuss": true, "message": "..." }
  ```
* The front reads `response.accessToken` and `response.expiresIn`. Both are `undefined`.
* Effect: `src/features/auth/api/register.ts` sends `Authorization: Bearer undefined`, and
  `Person/Register` returns **401**. A new user cannot register. A returning user gets a broken session.
* Fix: read `response.token`. There is no `expiresIn` in this response; the token is valid for
  1 day (`BearerTokenExpiration`), or read `exp` from the JWT.
* Note: `GetToken` (password login) **does** return `accessToken` and `expiresIn`. Keep `login.ts` as it is.

## F2 — Critical: create-order stops at the "sender location" map step

* File: `src/hooks/react-query/use-query-geoaddressing.ts`.
* The step calls `https://jetgw.tipax.ir/odata/Tipax/CmnCities/Tipax.ReverseLocationToAddress(...)`
  with `REACT_APP_MAP_TIPAX_TOKEN`. The call returns **401** (the token in `.env` has expired),
  and "next step" does not move on.
* Fix: get a valid token, and let the user continue (type the address) when the geocoder fails.
* Security: `REACT_APP_*` values are built into the public JS bundle. The Tipax tokens in `.env`
  are visible to every visitor. Move these calls behind the API, or use a token meant to be public.
  The same applies to `REACT_APP_TIPAX_TRACKING_TOKEN` (F9).

## F3 — High (agree first): option list shape — `GET /api/Order/GetOptions`

* The front expects (`src/types/create-order.ts`, `IgetOptions`):
  `{ providerId, providerName, orderOptions[], pickupDates[{ id, description, times[] }] }`.
* The API returns a flat list:
  ```json
  { "data": [ { "id": 1, "providerName": "تیپاکس", "delivereDate": "1405/08/10",
                "baseAmount": 0, "packageAmount": 0, "taxAmount": 120000, "totalAmount": 1320000 } ] }
  ```
* The API has no pickup dates or pickup times today.
* Decide with the API team: the front groups by `providerName` and drops pickup time, or the API adds the grouping and pickup data.

## F4 — High (agree first): select option + payment is one call in the API

* Front flow now: `SelectOption {orderId, orderInqueryOptionId, pickupId}` → then
  `SelectPaymentType {paymentType, orderId, pickupOptionId, channel, callbackUrl}`.
* API now: **one** call, `POST /api/Order/SelectOption`, body:
  ```json
  { "orderId": 2, "orderInqueryOptionId": 1, "channel": "pwa", "callbackUrl": "https://<front-origin>/app/..." }
  ```
  It returns `{ requestId, referenceCode, paymentLink }`.
* With the current front body the API returns **400** (`Channel` and `CallbackUrl` are required).
* `POST /api/Order/SelectPaymentType` **does not exist** (405).
* Files: `src/features/create-order/api/select-option.ts`, `src/features/create-order/api/pay.ts`.
* Decide with the API team which flow is correct, then align both sides.

## F5 — High: payment callback and verification

* File: `src/features/create-order/api/pay.ts`.
* `callbackUrl` is hard-coded to `http://app.logiwall.ir/app/home` (plain `http`, not from env).
  The API accepts only origins in `Payment:AllowedCallbackOrigins`. Read the URL from config and use `https`.
* **Nobody calls `POST /api/Payment/VerifyPayment`.** After the bank redirects back, the front must
  call it, or the order stays "payment pending" forever. Add a callback page that calls it and shows the result.
  Agree on the exact body with the API team (`VerifyPaymentRequestDto`).

## F6 — High (agree first): orders list shape — `POST /api/Order/GetOrders`

* The front expects `data: { totalCount, result: [...] }` with `statusName`, `originCityName`,
  `destinationCityName` (`src/types/orders.ts`).
* The API returns `data: [ ...orders ]`: no `totalCount`, no `*Name` fields.
* Effect: `/app/orders` is empty and MUI `Pagination` gets `count = NaN` (console error).
* Decide with the API team who adds the wrapper and the names.

## F7 — High: change password endpoint does not exist

* `src/features/profile/api/change-password.ts` calls `POST /api/Person/ChangePassword` → **405**.
* Hide the page until the API adds the endpoint, or agree on the contract with the API team.

## F8 — Medium: profile update sends a partial body

* `src/features/profile/api/change-user-info.ts` sends `{ firstName, lastName, nationalCode, email, gender }`.
* Today the API fails on this body (API bug B1, being fixed on the API side). No front change is needed
  for that. But confirm with the API team which fields the form owns, so the API does not clear
  `phoneNumber`, `kind` or `birthDate`.

## F9 — Medium: parcel tracking calls Tipax directly over HTTP

* `src/features/parcel-tracking/api/tracking.ts` posts to `http://jet.tipax.ir:200/...` with a
  token from `.env`.
* On an `https` site the browser blocks this (mixed content). The token is public in the bundle (see F2).
* Move the call behind the API, or use an `https` endpoint with a public-safe token.

## F10 — Low: cleanup

* `src/features/create-order/api/create-order.ts` sends `description: 'string'` (placeholder) and has a `console.log(mainPayload)`.
* `src/hooks/react-query/use-query-orders.ts` calls `GET /api/Order/GetOrders`, but the API only
  has `POST`. The hook is not used anywhere — delete it.
* Console warnings: `Autocomplete` gets `options = undefined` before the cities load (`SelectCity`,
  step one); missing `key` props in `StepTwo` (`LgToggleButton`) and `StepThree`; kebab-case CSS keys
  (`-ms-overflow-style`, `scrollbar-width`) in a style object.

---

## What works (no change needed)

| Call | Result |
|---|---|
| `POST /api/Identity/SendOtpCode` | 200 |
| `POST /api/Identity/GetToken` (password login) | 200, `accessToken` + `expiresIn` |
| `GET /api/Person/GetUserProfile` | 200 |
| `GET /api/Common/GetStaticDatas?Type=11` / `Type=12` | 200 |
| `POST /api/Order/CreateOrder` (with `parcelTypeId` 7 or 8) | 200, returns `{ orderId, orderNumber }` |

## How to reproduce locally

1. Run the API on `http://localhost:5030` with `Cors__AllowedOrigins__0=http://localhost:3000`.
2. In `Logiwall-Front`: set `REACT_APP_BASE_URL=http://localhost:5030` (environment variable or `.env.local`), then `npm start`.
3. Open `http://localhost:3000/auth/otp-login`, log in with OTP, and watch the Network tab.
