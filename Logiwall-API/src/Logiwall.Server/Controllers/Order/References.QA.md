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

Tipax packaging rows expose requiresPackaging=false for carrier codes 504/521 and true for regular packaging. Other categories return null. Frontends use this flag instead of hardcoded carrier IDs.

## Compatibility query filters

Use packageTypeId=0/1 to filter no-packaging/packaging choices. For content, packagingReferenceId limits results to the selected active provider-scoped packing type. Without a selected packing, the flag selects the union of compatible active packing types. For packaging, contentReferenceId limits results to the selected content type; the flag optionally separates regular and no-packaging choices. Omitted filters retain the existing catalogue response.

Reject invalid ranges, self-category filters, filters on status/service, contradictory flags, and foreign/inactive selected IDs. Return an empty successful list when no compatible active option exists. Authentication remains required. No carrier network request or database write is needed for catalogue filtering.
