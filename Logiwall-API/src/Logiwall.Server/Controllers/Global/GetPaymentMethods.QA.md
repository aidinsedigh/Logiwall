# QA: GET /api/Common/GetPaymentMethods

Public endpoint returns all eight ShopLink/Tipax seed payment meanings as shared IDs 71 through 78, with Persian titles.
Existing IDs 71/72 retain their mapping. Newly listed methods must survive frontend navigation without becoming 72.
Pricing maps all eight codes correctly. Methods 72-78 can register through the carrier with the selected mapping;
none fabricates a successful local online payment. Carrier acceptance/rejection and ambiguity are persisted normally.
No provider code, credential or provider-specific payment identifier is returned. No database or carrier call is required.
Unknown query parameters cannot change these choices. Concurrent reads produce the same contract.
Coverage: ShipmentTests.Shared_payment_choices_and_provider_service_catalog_are_separate.
