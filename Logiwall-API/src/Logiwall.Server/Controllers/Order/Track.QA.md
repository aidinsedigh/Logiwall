# QA: GET /api/Shipment/Track

Track a parcel by carrier and tracking code: `?providerCode=etipax&code=BA800197458IR`. The front no longer calls the carrier directly.
* Requires sign-in (no `[AllowAnonymous]`). The code must be a `ShipmentParcels.TrackingCode` of the user's own order with this provider, else 404 ParcelTrackingNotFound before any carrier call (the same answer as an unknown code, so other customers' codes cannot be probed). Rate limit `parcel-tracking`: 20 requests a minute for each user (`RateLimits:ParcelTrackingPerMinute`), then 429.
* `providerCode` must match an active `ServiceProviders.Code` and a registered `IShippingCarrier`, else 400 ShippingProviderUnavailable. It is case-insensitive.
* `code` must be 5 to 40 English letters or digits (spaces around it are removed), else 400 ParcelTrackingCodeInvalid before any carrier call.
* The response is one contract for every carrier (`ParcelTrackingResponseDto`): provider code and name, status, service name, registeredAt, sender and receiver (name, city, province), journey (sorted oldest first) and payment (method, total, pay-on-delivery, COD; rials as the carrier reports them, not rounded).
* Mobile numbers and addresses are not returned.
* An unknown code returns 404 ParcelTrackingNotFound. A carrier failure (network, timeout, 401, bad JSON, missing token) is logged with the provider code and returns 400 ProviderServiceUnavailable.

eTipax: `ETipaxCarrier.TrackAsync` calls the jet API (`Providers:ETipax:TrackingBaseUrl`, bearer `Providers:ETipax:TrackingToken`) at `/odata/Tipax/DspContractTrackings/Tipax.TrackingParcelWithDetail` with body `{"Code": "..."}`. An unknown code comes back as HTTP 200, `IsSuccess:false`, empty `Data`. Jalali times ("1405/08/16 12:38:10") are read as Iran time (+03:30).

Tests: ParcelTrackingTests (success shape, unknown code 404, another customer's code 404, unknown provider, invalid code, carrier failure, sign-in required) through FakeShippingCarrier; ETipaxCarrierTests (mapping of a real jet-test reply, unknown code, missing or rejected token, Jalali times).
Manual check (2026-11-07): jet-test returned barcode ba800197458ir in lower and upper case; ZZ999999999IR returned `IsSuccess:false`.
