# QA: /api/Shipment/Webhook

## Acceptance criteria

Require the ETipax shared secret independently of bearer authentication. Validate positive event IDs and bounded barcode/title.

## Positive and edge cases

Store each provider event ID once. Apply matching barcode to shipment parcel and OrderStatuses. Repeated deliveries succeed.

## Negative, authorization and failure cases

Missing/wrong secret: 401. Old IDs stay in events but cannot roll status back. Save unmatched events and reconcile after registration. Do not change payment/workflow status.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.
