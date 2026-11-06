# Implementation plan

## Enable all selected carrier payment methods, 2026-11-06

The user explicitly authorizes registration with every mapped carrier-settled method. Remove the temporary 73-78 restriction.
71 still requires verified online payment; 72-78 are submitted to ETipax with the frozen selected payment code.
Retain expiry, ownership, pending-payment, snapshot matching and idempotency checks. Do not mark carrier settlement as online payment success.
Verify with fake carrier integration tests only; never register a real order as verification.

## Remove invented pickup scheduling, 2026-11-06

Remove generated slots from all quotation responses and remove pickupId from selection input.
Selection confirms the price only; it must not require or assign a pickup time. Preserve historical nullable timestamps.
Remove the React time selector, slot generator, obsolete slot DTOs/tests, and scheduling instructions from the current frontend contract.
Keep legacy pickupOptions/pickupOptionId names only as payment quote identifiers for wire compatibility; they do not describe a schedule.

## Complete payment catalogue, 2026-11-06

Expose all eight methods in ShopLink's Tipax payment seed, preserving shared IDs 71/72 and adding shared IDs 73-78.
Map external codes 10/20/30/40/50/60/80/90 in ETipax only. Update frontend to retain every selected ID and display server titles.
Support pricing for all mapped methods. Existing settlement remains online/recipient cash; newly displayed wallet/credit/COD modes
must not spend a shared carrier account without a defined settlement policy. Return an explicit localized error before registration.
Verify catalogue completeness, mapping and frontend contract; no actual shipment/payment requests.

## Selected service quotation correction

Load service types independently through Shipment/References category=service. The client chooses a service and shared payment method,
then explicitly requests a price with serviceReferenceId and paymentType. Only the selected active provider service is priced.
Changing a choice clears the displayed quote but does not automatically call pricing. Preserve the existing price-response envelope.

## 2026-11-05: shared payment methods and carrier services

Select shared PaymentMethod 71/72 before quoting (default 72). Store it on the quote and map it only in the adapter.
Seed provider-scoped services from ShopLink's static migration. Use confirmed pickup/distribution defaults 10.
Remove service/payment environment dependencies; expose payment choices and service titles to the frontend.
Update contracts, active React flow, migrations and tests. Apply migrations only to local LogiwallDb.
Verify local pricing without sending real registrations/cancellations.

## Baseline and drift

The server targets net10.0. MyTipax supplies PostalServiceBase quotations. OrderStatuses is an order event history.
ShopLink uses token, pricing, create, cancel, reference endpoints and parcel-event webhooks under OM/v3.
Its ParcelStatus endpoint lists definitions, not current shipment statuses. A polling endpoint is not established.
codebase-memory-mcp and spec-driven skills are unavailable in this session; inspect source with rg and record this limitation.
Preserve the unrelated geoaddressing frontend change. Do not copy ShopLink modular-monolith or MediatR infrastructure.

### Task 1 — contracts and persistence
Introduce provider-scoped catalogues, shipment parcels, registration attempts and deduplicated events.
Add nullable parcel selections for existing orders and require them for ETipax quoting and registration.
Add a separate ETipax provider code; never reinterpret legacy MyTipax city mappings as ETipax codes.

### Task 2 — ETipax adapter
Implement token, reference, pricing, create and cancel contracts with bounded HTTP requests.
Use ShopLink payload casing and explicit units. Store accepted quotes for later registration.
Remove MyTipax from runtime registration, retain historical entities and migration data.

### Task 3 — orchestration and APIs
Expose reference lookup, admin synchronization, shipment register/read/cancel and authenticated webhook actions.
Claim registration atomically; freeze the exact request before posting. Persist outcome without client cancellation losing it.
Enforce ownership, quote selection and payment policy. Keep ambiguous results for reconciliation, not retries.

### Task 4 — background work
Run catalogue synchronization with SQL-backed lease and bounded intervals, disabled by default.
Reconcile received unmatched webhook events without fabricating carrier status polling.

### Task 5 — frontend and verification
Add provider packaging/content inputs to the active React order flow after checking its instructions.
Create migrations, provider contract tests and SQL integration tests. Run build and available tests.
Document environment keys, sync/bootstrap operations, webhook and the unresolved tracking-poll endpoint.

## Stop conditions
Do not call production registration. Do not manufacture provider reference identifiers or currency conversion.
An unavailable carrier tracking endpoint remains an explicit limitation, not a fake scheduled integration.

## Editable orders and rejected-registration recovery, 2026-11-06

User requirement: keep the same order editable and re-quotable until the carrier accepts registration.
1. Complete UpdateOrder with the CreateOrder fields plus orderId, ownership and shared validation.
2. Serialize edit, quote replacement, selection and registration on the order row. Reject stale quotations if the form changed during pricing.
3. Permit explicit retry after a definitive rejection; reuse the shipment and retain every attempt. Keep Submitting/Unknown and previously registered shipments locked.
4. Invalidate prior quotes/selections on edit; keep order identity and payment history. Carrier success, not price selection or payment, defines shipping finality.
5. Verify ownership, validation, repeat quotation, rejection/edit/retry, concurrency and finality using fake carrier and isolated SQL. Update frontend contract and action QA.

## Unified order and carrier status mapping, 2026-11-06

User request supersedes the old payment/selection status shown in order lists.
- Add a carrier-neutral persisted overall order status and nullable parcel mapping; preserve the legacy payment workflow state internally.
- Store mappings on provider-scoped status references, retaining original external code/title. Seed the 13 observed Tipax statuses using ShopLink's confirmed 59 registered/57 cancelled codes.
- Update lifecycle on edit/selection/registration/cancellation/webhook. Aggregate multiple parcels without declaring delivery until every parcel is delivered. Unknown provider codes remain stored and require mapping.
- Return the overall order status in lists and explicit carrier status plus operation state in shipment responses. Publish a general status catalogue and frontend migration notes.
- Generate migration/backfill existing rows, verify isolated SQL webhook/registration/list behavior and apply to local LogiwallDb only. No real carrier mutations.
