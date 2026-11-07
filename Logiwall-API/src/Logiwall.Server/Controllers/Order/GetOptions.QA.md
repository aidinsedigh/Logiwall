# QA: GET /api/Order/GetOptions

The active carrier path requires an owned order and a supported shared paymentType. `serviceReferenceId` is optional.
* Without `serviceReferenceId`: price every active service of every active provider. Return only the services the carrier accepts for this parcel, cheapest first, each with its own serviceTitle. A carrier rejection of one service (for example eTipax "segment not found": the service does not accept the pack type) is logged and skipped. If no service prices, return 400 with the most common carrier rejection message (ProviderServiceUnavailable when the carrier only failed) and keep the previous options.
* With `serviceReferenceId`: price only that service. A missing, inactive or wrong-category ID returns 400 ShippingServiceSelectionRequired before any carrier call. A carrier rejection returns its sanitized message.
* eTipax "نوع سگمنت" becomes ShippingServiceParcelTypeUnsupported; its "ارزش ... سگمنت" value ceiling becomes ShippingParcelValueTooHigh.
* eTipax "ارتفاع/طول/عرض مرسوله با بارکد  بیش از مقدار مجاز است." becomes ShippingParcelDimensionTooLarge (the carrier text has an empty barcode slot).

## Parcel limits checked before the carrier call

Checked on the eTipax test API on 2026-11-07. eTipax enforces only the weight cap. It ignores the catalogue dimensions and minimum weight, so Logiwall does not check them.
Correction from the front E2E run the same day: services 4 and 14 rejected a height of 99,999,999,999 cm ("بیش از مقدار مجاز"). So eTipax has a dimension cap, but its value is unknown. Logiwall maps the reply (ShippingParcelDimensionTooLarge) and still has no local dimension limit.
* Weight above the packaging's catalogue `maxWeight`: field error `orderParcels[i].weight`, key ShippingParcelOverweight, message shows the limit in kg.
* Packaging `packType` 70 (eco-pack): field error `orderParcels[i].packagingReferenceId`, key ShippingEcoPackPickupUnsupported. eTipax refuses customer pickup for eco-pack; Logiwall always quotes with pickup (pickupType 10).
* The value ceiling is per segment and not in the catalogue (box: 1–2 billion rials; envelope: under 1 billion). It is left to eTipax and mapped to a clear message.
Tests: NoPackagingTests (overweight and eco-pack field errors, no carrier call); OrderQuoteFlowTests (all services rejected returns the carrier reason); ETipaxCarrierTests (segment and value messages).
Each carrier price is rounded up to the next 1,000 rials before it is stored and returned (baseAmount and totalAmount); SelectPaymentType charges that stored amount.
No pickupDates, time slots or invented delivery promises are returned.
Invalid parcel references return indexed Persian field errors.
Carrier HTTP 400/422 returns sanitized validation details. Failed pricing preserves previous stored options.
Carrier-accepted, in-flight and uncertain shipments cannot be re-quoted. Payment state alone does not finalize carrier shipping. Concurrent quotes use order-row locking.
Another customer's order returns 404 InvalidOrder.
Tests: OrderQuoteFlowTests calls the HTTP endpoint through CarrierQuoteService and FakeShippingCarrier (service and payment method passed, all-services quote skips a rejected service, a rejected selected service returns the carrier message, failure keeps old options, parallel re-quotes leave one set); OrderOwnershipTests covers ownership; ShipmentTests covers snapshots and an unknown service ID; ETipaxCarrierTests covers the segment message.

Repeat pricing is allowed for draft/rejected carrier shipments, including orders marked Paid/PaymentPending. Carrier acceptance defines shipping finality. Reject Submitting/Unknown and previously accepted shipments. After pricing, compare the form modification timestamp under the order lock; return ShippingOrderChanged if an edit raced pricing. A new quote clears the previous selection and amounts.

## No carrier packaging

PackageTypeId=0 accepts a null PackagingReferenceId. Resolve an active provider-scoped no-packaging record with the content's packType. Tipax uses 504 (type 20) or 521 (type 60). Quote and register use the same resolved reference snapshot.
Reject missing/inactive compatible references before calling the carrier. Reject contradictory packaging flags and incompatible content. Return localized field errors. Mixed orders resolve each parcel independently.
