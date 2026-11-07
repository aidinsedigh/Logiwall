# QA: GET /api/Shipment/Services?providerCode=etipax

Authenticated users receive active service types scoped to the requested provider, using internal IDs and titles.
For eTipax only codes 4, 12, 13, 14, 15 are active (migrations `SelectableETipaxServices` and `DisableETipaxLtlService`; LTL (10) rejected every pack type on the eTipax test account on 2026-11-07).
This endpoint is independent of quotation and performs no external pricing call. It accepts no body.
The ID returned may be submitted as GetOptions.serviceReferenceId to price one service (omit it to price all); it is not the carrier's external service code.
Anonymous requests fail authentication. Unknown provider or no active service references returns an empty list.
Regression coverage: ShipmentTests.Shared_payment_choices_and_provider_service_catalog_are_separate.
