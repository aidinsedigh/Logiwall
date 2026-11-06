# QA: GET /api/Shipment/Services?providerCode=etipax

Authenticated users receive active service types scoped to the requested provider, using internal IDs and titles.
This endpoint is independent of quotation and performs no external pricing call. It accepts no body.
The ID returned is submitted as GetOptions.serviceReferenceId; it is not the carrier's external service code.
Anonymous requests fail authentication. Unknown provider or no active service references returns an empty list.
Regression coverage: ShipmentTests.Shared_payment_choices_and_provider_service_catalog_are_separate.
