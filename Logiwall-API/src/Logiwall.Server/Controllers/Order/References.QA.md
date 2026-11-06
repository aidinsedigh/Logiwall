# QA: /api/Shipment/References

Category service returns 13 seeded ETipax services scoped to the provider, using internal IDs and titles.
Remote reference synchronization must not deactivate or overwrite these static service definitions.

## Acceptance criteria

Authenticated users receive active internal reference IDs scoped to provider and category.

## Positive and edge cases

Packaging includes packType; content/status use the same response envelope. Removed carrier items remain stored but inactive.

## Negative, authorization and failure cases

Unknown categories return a Persian validation error. No credentials or raw catalogue metadata are returned. Check no cross-provider IDs leak.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.
