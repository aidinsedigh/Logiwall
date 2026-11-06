# QA: /api/Shipment/Get

## Acceptance criteria

Return provider code, shipment state and per-parcel tracking/status only to its owner.

## Positive and edge cases

A registered shipment returns stored codes. Unmatched older events cannot roll back them.

## Negative, authorization and failure cases

Anonymous: 401. Another owner or missing shipment: 404. Never expose request/response snapshots or credentials.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.

Carrier status: top-level statusCode/statusTitle and parcels[].statusCode/statusTitle retain provider values. Parcel mappedOrderStatus/name provide general equivalents. operationState controls submission/cancellation only; state is its legacy compatibility alias. Mixed parcel statuses leave top-level provider status null.

Regression coverage (2026-11-06): courierStatusCode/courierStatus appear at shipment and parcel level. Older confirmed registrations with missing initial status use carrier reference 59. Unknown outcomes keep missing status null. Original webhook titles and unknown external codes remain intact.
