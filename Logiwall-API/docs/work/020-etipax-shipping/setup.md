# ETipax setup and verification

Frontend request/response contracts and examples: [frontend-api.md](frontend-api.md).

## Configuration

Use server environment variables or dotnet user-secrets. Never put credentials in frontend env files.
These keys bind to Providers:ETipax. Empty defaults keep external jobs disabled.

| Environment key | Value |
|---|---|
| `Providers__ETipax__BaseUrl` | Confirmed account URL; default `https://omtestapi.tipax.ir` is ShopLink's test URL |
| `Providers__ETipax__Username` | ETipax account username |
| `Providers__ETipax__Password` | ETipax account password |
| `Providers__ETipax__ApiKey` | ETipax account API key |
| `Providers__ETipax__WebhookSecret` | Long random secret agreed with the carrier |
| `Providers__ETipax__ReferenceSyncEnabled` | Enable after credentials and migrations are ready |
| `Providers__ETipax__ReferenceSyncMinutes` | Default 1440, bounded 10–10080 |
| `Providers__ETipax__ReconcileEventsEnabled` | Enable after migrations; processes received events and interrupted attempts |
| `Providers__ETipax__TrackingBaseUrl` | eTipax "jet" tracking host; default `http://jet-test.tipax.ir:81` (test). Set the production host before release |
| `Providers__ETipax__TrackingToken` | Long-lived jet bearer token for `GET /api/Shipment/Track`. Secret: user-secrets or environment only |

Payment methods 71/72 are shared Logiwall values. The adapter maps them to ETipax 10/20; pickup and distribution are 10, from ShopLink. Service references are seeded in category service; no mode or service-ID environment variables are required.
The Jet geolocation JWT is not an ETipax account token.

## Database and bootstrap

Review migrations `ETipaxShipping`, `ShippingEventReconciliationSchedule` and `CarrierServicesAndSharedPayment` before deployment.
They create shipment/reference/attempt/event/lease tables and import 678 cities for each existing origin/destination category.
They preserve existing IDs where a city is unambiguous, preserve historical MyTipax mappings, disable MyTipax, and add ETipax.
The immutable city seed is embedded in Infrastructure, so it also ships in published output.
Down removes new shipment schema but intentionally retains imported cities and mappings to preserve identifiers.
Do not roll back after real shipments without a separate data recovery plan.

From `src/Logiwall.Server`, with a deliberately selected database:

```powershell
dotnet tool restore
dotnet ef database update --project ../Logiwall.Infrastructure --startup-project . --configuration Release
```

Migrations have been applied to local LogiwallDb. Production was not changed. Tests use a separate random database and delete it afterwards.
Use `POST /api/Shipment/SynchronizeReferences` as an admin, or enable the catalogue job.
`GET /api/Shipment/References?category=packaging&providerCode=etipax` returns internal IDs, not carrier codes.
Categories are packaging, content, status and service. The 13 ETipax services come from ShopLink static seeds, not a remote service endpoint. Failed/empty sync never deletes the previous catalogue.

## Frontend and shipment flow

Pickup scheduling has been removed: quotation returns no pickupDates and SelectOption requires no pickupId.
Selection confirms an order price only; it neither books nor fabricates a collection time.

Current quotation contract: load `GET /api/Shipment/Services?providerCode=etipax`, choose an internal service ID and shared payment method,
then call `GET /api/Order/GetOptions?orderId=17&serviceReferenceId=<id>&paymentType=72`.
Only that selection is priced. No catalogue fan-out or automatic pricing on form-choice changes takes place.

The active frontend updated here is `E:/Projects/Logiwall-Bulletproof/logiwall-app`.
Each parcel sends packagingReferenceId and contentReferenceId alongside existing fields.
The quote stores the exact accepted request. Selection/payment initiation reject an expired quote.
After verified online payment, `POST /api/Shipment/Register` with `{ "orderId": 123 }` registers it.
Already verified payments can complete registration after the original quote expiry.
Choose the shared payment method before quotation. Carrier collection requires an unexpired selected quote with method 72; the front's 72 path now calls Register and waits for a real outcome.
Order details show carrier state and parcel tracking codes, and offer registration for an unshipped order.
Errors preserve the order and never display a fake shipping success.

The pre-existing SEP verification method is a stub. This work does not fabricate successful online payments.
The online path remains blocked until that separate gateway verification contract is implemented.
Carrier cancellation uses `POST /api/Shipment/Cancel`; it does not refund payments or change payment state.

## Webhook and jobs

Register `POST /api/Shipment/Webhook` with `X-Tipax-Webhook-Secret` at the carrier.
Body: parcelEventId, parcelEventTypeId, barcode, optional eventTypeTitle/barcodeTitle.
Bearer login cannot substitute for the webhook secret. Event IDs are deduplicated per provider.
The job retries unmatched events for 2 days after they arrive; older unmatched events stay stored but are not retried. It marks operations left Submitting for over 10 minutes as Unknown.
A Tipax login failure (network, missing credentials, no token) happens before the order request, so it is a definite rejection the customer may retry. It never creates Unknown. The access token is cached in ProviderTokenCache and dropped on a 401.
No registration retry or fabricated status polling takes place. Unknown results need investigation using traceCode and attempts.

## Verification commands

```powershell
dotnet test tests/Logiwall.UnitTests -c Release
# Optional when Docker is unavailable: SQL Server instance only; the fixture always creates its own random catalog.
$env:LOGIWALL_TEST_SQLSERVER='Server=localhost;Integrated Security=True;TrustServerCertificate=True'
dotnet test tests/Logiwall.IntegrationTests -c Release
```

External providers are fakes in all tests. No real shipment, cancellation or payment was sent.
Frontend validation: `npx --no-install tsc --noEmit` and focused React tests.
