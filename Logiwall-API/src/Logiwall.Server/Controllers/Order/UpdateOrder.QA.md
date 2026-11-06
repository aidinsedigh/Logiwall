# QA: POST /api/Order/UpdateOrder

Requires customer authentication and ownership. Full CreateOrder body plus positive orderId; all inherited validation applies. Return the existing orderId/orderNumber in the standard ServiceResponse. Never change owner, identity or payment records from the request.

- Edit sender/receiver, city, addresses, coordinates, packaging/content and the entire parcel list before carrier acceptance.
- Clear old quotes, accepted price and amounts. Register requires a new quote and selection after editing.
- Rejected carrier registration can be corrected and submitted again explicitly. Preserve shipment identity and attempt history; detach rejected parcel links before replacing parcels.
- Previously Registered/Cancelled shipments, Submitting and Unknown return 409. A different owner returns 404. Missing/invalid fields or empty parcel lists return 400 and leave the order unchanged.
- Repeated quotation is allowed until carrier acceptance. Payment state alone does not finalize shipping.
- Edit/pricing/selection/registration lock the same order row; a price calculated from an older form cannot overwrite edited data.

Automated coverage: OrderEditTests (HTTP validation/ownership, invalidation, rejection/retry/history, shipping finality, stale concurrent pricing), ShipmentTests (parallel registration and unknown outcomes).

Allow changing a draft parcel between carrier packaging and no carrier packaging. No packaging uses PackageTypeId=0 and PackagingReferenceId=null. Invalidate the previous quote and resolve a compatible active no-packaging carrier reference on the next quotation.
