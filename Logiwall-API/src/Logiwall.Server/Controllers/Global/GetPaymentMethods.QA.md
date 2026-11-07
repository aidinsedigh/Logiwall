# QA: GET /api/Common/GetPaymentMethods

Public endpoint returns the five selectable methods 71, 72, 73, 75, 77 with Persian titles (`Constants.SelectablePaymentMethods`).
74 and 78 (credit) and 76 (wallet on delivery) are not listed. GetOptions rejects them with `ShippingPaymentMethodMismatch`.
Existing IDs 71/72 retain their mapping. Listed methods must survive frontend navigation without becoming 72.
The adapter still maps all eight codes, so a saved quote with 74/76/78 can register. Methods 72-78 can register through the carrier with the selected mapping;
none fabricates a successful local online payment. Carrier acceptance/rejection and ambiguity are persisted normally.
No provider code, credential or provider-specific payment identifier is returned. No database or carrier call is required.
Unknown query parameters cannot change these choices. Concurrent reads produce the same contract.
Coverage: ShipmentTests.Shared_payment_choices_and_provider_service_catalog_are_separate,
ShipmentTests.Pricing_rejects_payment_methods_disabled_at_the_carrier.
