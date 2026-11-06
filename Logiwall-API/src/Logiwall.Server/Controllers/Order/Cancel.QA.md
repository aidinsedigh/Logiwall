# QA: /api/Shipment/Cancel

Carrier 400/422 validation returns localized HTTP 400 field details after the attempt result is stored.
Definitive cancellation rejection leaves the shipment Registered. Network/server ambiguity remains Unknown; do not retry blindly.

## Acceptance criteria

Only the shipment owner can cancel a Registered shipment.

## Positive and edge cases

Successful cancellation changes carrier state to Cancelled; a repeated request returns it without another call.

## Negative, authorization and failure cases

Concurrent register/cancel is serialized. Rejection keeps Registered. Timeout becomes Unknown. Payment remains unchanged; no refund is invented.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.
