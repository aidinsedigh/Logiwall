# QA: /api/Shipment/SynchronizeReferences

## Acceptance criteria

Only AdminsOnly can synchronize all active carrier catalogues.

## Positive and edge cases

Two syncs preserve internal IDs; disappeared codes deactivate without deleting order references.

## Negative, authorization and failure cases

Carrier failure/empty catalogue/malformed data leaves existing catalogue intact. Test duplicate codes and parallel syncs. Ordinary user: 403; anonymous: 401.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.
