# Logiwall-Frontend — fixes found in the E2E test, 2026-11-07

The front was driven in Chrome (DevTools MCP) against the local API. This file lists what the
**front** must change. Backend problems from the same run are fixed in `Logiwall-API`. See
"API changes from this run" at the end; two of them change what the front should show.

* Front tested: `D:\Own Projects\Logiwall-Frontend\Logiwall`, branch `v2` @ `64e88b1`, `npm start` on `:3000`.
* API: `Logiwall-API` `main` @ `08dad9b`, Development, `:5030`, local SQL Server.
* Result: 70 scenarios. 35 pass, 28 fail, 7 blocked.

Severity: **High** = a feature is broken or the user is misled. **Medium** = wrong data, a blocked
user group, or a security risk. **Low** = cleanup or polish. File paths are relative to the front repo.

---

## F1 — High: adding a second parcel crashes the create-order page

* Steps: new order → step 2 → scroll down → click `+ اضافه کردن مرسوله`.
* Actual: the page shows "Something went wrong!". Dev overlay:
  `TypeError: Cannot read properties of undefined (reading 'isFetching')`. All entered data is lost.
  Reproduced 2 of 2 times. Multi-parcel orders are impossible.
* Cause: `src/features/create-order/components/StepTwo.tsx:143` reads `query.isFetching` without a
  null check. `references` (`useQueries`, lines 43–54) is built from `useWatch` values. They are one
  render behind the `useFieldArray` rows, so the new row has no query yet.
* Fix: `query?.isFetching` (and `query?.data`), or build the query list from `fields`.

## F2 — High: change password shows "success" when it fails

* Steps: `/app/change-password` → wrong current password → any new password → submit.
* Actual: two toasts at once, `رمز عبور صحیح نیست` and `رمز عبور با موفقیت تغییر داده شد`,
  and an uncaught `AxiosError 400` overlay. Reproduced 2 of 2 times.
* Cause: `src/features/profile/api/change-password.ts` calls `mutateAsync(payloads)` without
  `await`, then calls `toast.success` at once. The rejection is never handled.
* Fix: `await mutateAsync(...)` inside `try`. Show `errorMessage(error)` in `catch`. Show success only after 200.

## F3 — High: the quote step hides the API's reason

* Steps: step 2 height `99999999999` cm → finish the steps → step 8, choose a payment method.
* Actual: the API answers 400 with a clear Persian `message` (since today:
  `ShippingParcelDimensionTooLarge`, see API changes). The page shows only
  `دریافت هزینه‌های ارسال انجام نشد.` and a Retry button that fails the same way after about 10 s.
* Cause: `src/features/create-order/components/StepEight.tsx:36` shows fixed text.
* Fix: show `errorMessage(query.error)`. When the error has `payload.details[].name`
  (for example `orderParcels[0].weight`), offer "edit parcel" that goes to step 2.
  Also add a sane upper limit to `parcelSchema` (`src/features/create-order/api/order-payload.ts:15`),
  so an absurd size is caught at step 2. Agree on the number with the API team. eTipax has a cap, but its value is unknown.

## F4 — Medium: order-number filter sends `null`

* Steps: `/app/orders` → filter → order number `۷` (Persian digit) or `abc` → apply.
* Actual: the front sends `{"propertyName":"id","value":null}`. Before today the API answered 500.
  The field then shows `NaN`.
* The API now skips a condition that does not convert. So the same input now returns **all** orders,
  which still looks wrong to the user.
* Fix: convert Persian and Arabic digits to Latin digits. Validate digits only, with a field message.
  Do not send the condition when the field is empty. Never write `NaN` back into the field.

## F5 — Medium: carrier name is wrong on every order

* Actual: cards show `شرکت پستی #2`; the details dialog shows the generic `شرکت حمل`. The Tipax logo never shows.
* Cause: the API sends `serviceProviderTitle` (for example `تیپاکس`).
  `src/features/Orders/components/Order.tsx:32` reads `providerName`, and
  `src/features/Orders/components/order-details.tsx:197` reads `serviceProviderName`. Neither field exists.
* Fix: read `serviceProviderTitle`.

## F6 — Medium: duplicate entries in "parcel contents"

* Actual: with "no packaging", `محتویات مرسوله` lists `عمومی` twice and `موبایل، تبلت، لپ تاپ…` twice.
  Each pair has the same `title` and a different `packType` (20 and 60). The user cannot choose correctly,
  and the choice changes price and acceptance.
* This is by design on the API side (`frontend-api.md` §2.3: no-packaging contents are filtered by the
  `packType` of the `requiresPackaging: false` packaging records, today types 20 and 60).
* Fix: group or label the contents by `packType`. Use the title of the matching
  `requiresPackaging: false` packaging record from `Shipment/References?category=packaging`.

## F7 — Medium: map address search finds no streets

* Actual (Parsimap `geocode/forward`): `میدان ونک` → 0, `خیابان ولیعصر` → 0, `تهران، ونک` → 5.
  `ونک` alone returns a different town (`شهر ونک`).
* Cause: `src/lib/parsimap/index.ts` `searchAddress` sends only `search_text`.
* Fix: put the selected city in front of the query (`${city}، ${query}`), or pass a location bias.

## F8 — Medium: spaces-only name fails two steps later

* Steps: step 5 last name = three spaces → step 5 accepts → step 7 submit.
* Actual: toast `مشخصات فرستنده و گیرنده را کامل کنید`. It does not name the field, and the field is two steps back.
* Cause: the name regex `/^[\u0600-\u06FF\s]+$/` accepts spaces. `buildOrderPayload` trims and
  throws later (`order-payload.ts:21-24`).
* Fix: `.trim()` in the step 5 and step 7 schemas.

## F9 — Medium: plate and unit limited to 1–199

* Actual: unit `200` → `واحد را بدرستی وارد نمایید`. Many real plates are above 199.
* Cause: `NumberFieldSchema('پلاک', 1, 199, …)` and the same for unit (`src/features/create-order/api/create-order.ts`, step 5 schema).
* Fix: raise the maximum (the API allows 50 characters), or accept any positive number.

## F10 — Medium: refresh loses the whole wizard

* Actual: a reload on any step returns to step 1 with empty fields. `orderId` stays in the URL,
  so the next submit silently updates that order.
* Fix: keep the draft in `sessionStorage`, or reload the saved order when `orderId` is present.
  At least warn before unload.

## F11 — Medium: password fields show the password

* `src/features/profile/components/change-password.tsx`: both inputs are `type="text"`, `autocomplete="off"`.
  There is no confirm field and no rule before submit (new password `1` goes to the API).
* Fix: `type="password"`, `autocomplete="current-password"` / `"new-password"`, a confirm field, minimum length 6 (`ChangePasswordRequestDto`), and show the API field errors
  for the Identity password policy (`AppSettings:IdentitySettings:Password*`).

## F12 — Medium: profile form accepts an invalid national code and e-mail

* Actual: national code `1234567890` (wrong check digit) and e-mail `a@b` show no error.
* The API now rejects both with field errors (see API changes). The form must show them.
* Fix: add the same rules to the form: 10 digits with a valid check digit, and an e-mail with a dot in
  the domain. Map `payload.details[].name` (`nationalCode`, `email`) to the field errors.
  Also check the register form (`nationalCode` has the same rule now).

## F13 — Medium (accessibility): "add parcel" and the packaging toggles

* `+ اضافه کردن مرسوله` is a `div` with `onClick` (`StepTwo.tsx:208`), with no role, no tabindex and
  no key handler. Keyboard users cannot add a parcel. At 745×667 it sits under the sticky footer, and a
  click before scrolling does nothing. Fix: use `Button`, and pad the form bottom by the footer height.
* The packaging toggles have `aria-label` "right aligned" and "justified" (copied from the MUI demo).
  Fix: use the Persian option names.

---

## Low

* **F14 — city picker:** focus goes to the dialog container, so text typed right after opening is
  lost. Enter in the search box does not pick the match. Exact matches are not listed first.
* **F15 — English browser tooltip:** "Please match the requested format." shows next to the Persian
  error on number fields (native `pattern`). Remove `pattern`, or call `setCustomValidity`.
* **F16 — wrong label:** the receiver form labels the last name `نام خانوادگی فرستنده`.
* **F17 — landline as mobile:** `03132221111` passes `شماره تلفن همراه`. Require `09…`.
* **F18 — OTP phone:** `0812345` goes to the API, and the answer is the generic `داده های درخواست معتبر نمیباشند.`.
  Validate in the form and show `payload.details` field errors.
* **F19 — fractional rials:** order #2 shows `۱۰٬۳۹۵٬۹۵۳٫۶۵ ریال` (old data from before API quote rounding). Round rials when you display them.
* **F20 — offline:** with no network, the tracking button spins forever (React Query pauses
  mutations offline). Show an offline message, or set `networkMode: 'always'` for mutations.
* **F21 — leftover route:** `/app/parcel-tracking/result` renders empty sender/receiver and a
  hard-coded `تیپاکس اکسپرس پارس`. Delete the route or redirect it to `/app/parcel-tracking`.
* **F22 — Help and About:** `src/features/profile/components/index.tsx:33-34` send both to `/app/change-password`.
* **F23 — console:** React "unique key" error in `StepThree`. "Blocked aria-hidden … descendant retained focus"
  when a picker opens. Deprecated `apple-mobile-web-app-capable` meta.
* **F24 — duplicate request:** step 2 loads `Shipment/References` (content) twice (`staleTime: 0`).
* **F25 — icons from a third party:** icons load at runtime from `api.iconify.design`. One request failed
  (`ERR_CONNECTION_RESET`). An offline PWA shows no icons. Bundle the used icons.
* **F26 — English screen-reader text:** "Close toast", "Notifications alt+T", "Zoom in/out", and the
  error screen "Something went wrong!" (which also has no Back or Retry action).

## UX notes (not bugs)

* From step 8, editing the parcel needs five Back clicks through two maps. Add "edit parcel".
* Orders in `تکمیل نشده` have only "details". Add "continue" or "pay".
* `تیپاکس لجستیک معکوس` (reverse logistics) is offered, and is the cheapest, for a normal forward
  shipment. Confirm with the business before release.
* Sender details are not pre-filled from the profile.
* Step 1 errors are only a toast, not attached to the city fields.
* Step 3 summary leaves out the contents and uses Latin digits.
* The parcel value field shows `0` with no visible label.
* The same map pin is accepted for sender and receiver.
* Tracking "not found" is only a 4-second toast.
* The order-status tab is not kept in the URL.
* The profile edit form has no Cancel button. The side menu has no close button.
* Logout has no confirmation. The login page has no "forgot password" link.

## Security note (no front change now)

Logout only deletes the local token. The API does not revoke it, so a copied token works for up to
1 day after logout. The API team recorded this as an open risk (user decision 2026-11-07).
If the API later adds a sign-out endpoint, call it from `src/hooks/use-auth-logout.ts`.

---

## API changes from this run (Logiwall-API, 2026-11-07)

| Change | Front effect |
|---|---|
| `GetOrders`: a filter value that does not convert (for example `null` for `id`) is skipped. Before: 500. | See F4. Validate before you send. |
| `GetOptions`: the eTipax "… بیش از مقدار مجاز است" size reply becomes `ShippingParcelDimensionTooLarge`. | See F3. Show the message. |
| `UpdateUserProfile`, `Register`: `nationalCode` must have a valid check digit (`InvalidNationalCode`). Profile `email` needs a dot in the domain. Profile `firstName`, `lastName` are required. | See F12. |

## Blocked in this run

* OTP login, redirect to `returnTo` after login, registration: not run (sending an OTP needed approval).
* Payment at the gateway and `/app/payment-result`: SEP stage returned 401 on `Payment/token` (API config).
* Non-prepaid payment methods: not run, because they book a real eTipax pickup.
* Account lockout, profile save, tracking a real parcel: not run (shared account, real personal data, no tracking code).

## What works

Route guard with `returnTo`. Password login errors (generic message, masked field). Logout syncs
across tabs. Order details dialog (Esc, focus return, text-safe rendering). Empty states. Skeleton
loading on Slow 3G. Double-click protection on create order and final submit. Map pin
outside the selected city is rejected. No horizontal overflow at 375, 768 and 1440 px. 404 page.

## How to reproduce locally

1. Run the API: `dotnet run --project src/Logiwall.Server` in `Logiwall-API` (`http://localhost:5030`, Development uses OTP code `12345`, no SMS).
2. In `Logiwall-Frontend/Logiwall` (`v2`): `REACT_APP_BASE_URL=http://localhost:5030` in `.env`, then `npm start`.
3. Open `http://localhost:3000`, sign in, and follow the steps above with the Network tab open.
