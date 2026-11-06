# QA: GET /api/Order/GetOptions

The active carrier path requires an owned order, a selected active serviceReferenceId and a supported shared paymentType.
Price only that provider/service and preserve serviceTitle, amounts and selected payment method in the response.
No pickupDates, time slots or invented delivery promises are returned.
Missing/inactive/wrong-category selections fail before carrier calls. Invalid parcel references return indexed Persian field errors.
Carrier HTTP 400/422 returns sanitized validation details. Failed pricing preserves previous stored options.
Carrier-accepted, in-flight and uncertain shipments cannot be re-quoted. Payment state alone does not finalize carrier shipping. Concurrent quotes use order-row locking.
Another customer's order returns 404 InvalidOrder. A missing serviceReferenceId returns 400 before any carrier call.
Tests: OrderQuoteFlowTests calls the HTTP endpoint through CarrierQuoteService and FakeShippingCarrier (service and payment method passed, failure keeps old options, parallel re-quotes leave one set); OrderOwnershipTests covers ownership; ShipmentTests covers snapshots.

Repeat pricing is allowed for draft/rejected carrier shipments, including orders marked Paid/PaymentPending. Carrier acceptance defines shipping finality. Reject Submitting/Unknown and previously accepted shipments. After pricing, compare the form modification timestamp under the order lock; return ShippingOrderChanged if an edit raced pricing. A new quote clears the previous selection and amounts.

## No carrier packaging

PackageTypeId=0 accepts a null PackagingReferenceId. Resolve an active provider-scoped no-packaging record with the content's packType. Tipax uses 504 (type 20) or 521 (type 60). Quote and register use the same resolved reference snapshot.
Reject missing/inactive compatible references before calling the carrier. Reject contradictory packaging flags and incompatible content. Return localized field errors. Mixed orders resolve each parcel independently.
