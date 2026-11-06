# Cycle 020 tasks

Frontend API handoff: [frontend-api.md](frontend-api.md). Server setup: [setup.md](setup.md).

2026-11-06 settlement restriction removed by explicit user request (supersedes earlier temporary restriction):
- Register permits mapped carrier-settled methods 72-78, including method 73. Eligibility and actual settlement are decided by ETipax.
- Retained expiry, pending online-payment protection, snapshot mapping, ownership and no-duplicate registration controls.
- Method 71 still requires verified local online payment. No fabricated paid rows are created for carrier settlement.
- All 22 shipment integration cases passed, including each newly enabled method and repeat-registration idempotency.
- Updated frontend contract/QA; restarted local host. No real order was registered during verification.

2026-11-06 pickup scheduling removed at the user's request:
- Deleted PickupSlots and its obsolete DTOs/tests. Quotation no longer includes pickupDates.
- SelectOption accepts only orderId/orderInqueryOptionId and does not assign PickupDateTime.
- Removed the React time selector and the requirement to choose/send a time. Existing nullable historical timestamps are retained.
- Updated frontend handoff and QA. No database migration or external shipping call was needed.
- Verification: 44 unit tests passed, 103 SQL integration tests passed with 1 existing skip, 4 focused React tests passed; TypeScript passed.

2026-11-06 complete payment catalogue: all eight ShopLink/Tipax payment meanings are exposed as shared IDs 71-78.
Adapter maps them to external 10/20/30/40/50/60/80/90. Frontend preserves and displays every server-returned option.
All eight support price serialization; newly listed settlement modes remain blocked before carrier submission until their payment policy is defined.
Verified running local GET Common/GetPaymentMethods returns 8 Persian entries. No migration or real shipment/payment was performed.
Validation: 53 unit tests passed, 16 shipment integration tests passed, 4 focused frontend tests passed; frontend typecheck passed.

Selected-service correction (supersedes earlier fan-out behavior):
- Separate authenticated GET Shipment/Services lists provider service types without pricing.
- GetOptions receives serviceReferenceId plus shared paymentType and prices only that active provider service.
- Removed adapter catalogue fan-out. The React form selects both inputs before an explicit pricing action.
- Live read-only pricing for local order 17 and selected ground service 4 returned exactly one quote.
- Unit tests: 47 passed; full SQL run: 97 passed, 1 existing skip; focused frontend tests: 4 passed.

2026-11-05 shared-payment/service update completed:
- Shared methods 71/72 exposed by Common/GetPaymentMethods, selected before quoting and frozen on OrderInqueryOptions.PaymentMethod.
- ETipax maps 71 to 10 and 72 to 20; confirmed pickup/distribution defaults are 10. Removed obsolete environment mode/service settings.
- Seeded 13 provider-scoped service references from ShopLink. Remote reference sync preserves them.
- Quote responses include serviceTitle/paymentType. Payment initiation and registration reject mismatched settlement snapshots.
- Active React flow re-quotes on method change and carries the accepted method to the pay step.
- Migration applied to local LogiwallDb; running local host restarted on port 5030. Production unchanged.
- Live read-only ETipax pricing for order 17 succeeded, returning 13 service quotes. No real shipment registration/cancellation was sent.
- Verification: 47 unit tests passed; 95 SQL integration tests passed, 1 existing skip; 4 focused React tests passed.
- EF pending-model check clean. Frontend TypeScript validation recorded separately.

2026-11-05 validation follow-up: distinguish missing packaging/content per parcel from upstream validation.
Pricing and mutation HTTP 400/422 responses expose localized known field details, without raw upstream diagnostics.
Mutation outcomes are persisted before returning validation errors. Unit coverage: 44 passed; shipment SQL coverage: 12 passed.

- [x] Task 1 — contracts and persistence
- [x] Task 2 — ETipax adapter
- [x] Task 3 — orchestration and APIs
- [x] Task 4 — reference and event reconciliation jobs
- [x] Task 5 — frontend, migrations, tests and QA

Source baseline inspected. No production mutations authorized by this cycle.

## Verification, 2026-11-04

- Release server build: passed (also built by integration tests). Existing warnings remain.
- Unit tests: 42 passed.
- SQL Server integration tests: 92 passed, 1 pre-existing skip. New shipment tests cover ownership, concurrency, unknown outcomes,
  webhook auth/dedup/order, unmatched-event reconciliation, catalog ID stability, quote snapshots, cancellation and carrier collection modes.
- Docker was unavailable. An opt-in local SQL Server fixture created a random LogiwallTests catalog, ran all migrations and deleted it.
- Frontend TypeScript: passed. Focused React tests: 6 passed across order details, shipment state and payment/registration hooks.
- EF pending-model check: no changes. Git diff whitespace check: passed.
- The broader Server.slnf --no-restore build was not a valid check: pre-existing Blazor assets had no net10.0 target.
  Server and both test projects were built and checked separately.

## Self-review

Applied order row locks to service selection, payment initiation, quote replacement and shipment claims.
Carrier I/O happens after the registration attempt commits. A disconnected client cannot discard the outcome.
Interrupted operations become Unknown; no automatic resubmission. Settlement policy verifies the stored payment mode.
Catalogue fetches complete before changing stored references. Events use provider-scoped deduplication and a consistent lock order.
Unmatched events rotate by LastReconciledAt to avoid starving newer events.
Raw personal-data snapshots are not returned through customer APIs or intentionally logged.

Preserved unrelated changes in both frontend workspaces. Actual UI changes are in E:/Projects/Logiwall-Bulletproof/logiwall-app;
the older sibling Logiwall-Front geoaddressing change was not modified by this cycle.
No commits, deploys, live carrier requests or migrations to application/production databases were performed.

## Rollout items

- Configure ETipax credentials and confirmed service/payment/pickup/distribution IDs; see setup.md.
- Confirm monetary units under ADR 0001 before real price/payment operations.
- Apply reviewed migrations to the chosen application database and bootstrap reference catalogues.
- Configure the webhook and enable the two leased jobs.
- Existing SEP verification is still a stub. Online-paid shipping stays blocked until gateway verification is implemented separately.
- Per-shipment polling requires a carrier API contract. Current jobs process received events and synchronize catalogues only.
- Unknown/rejected operations and missing tracking codes require operator investigation; no blind retry endpoint was added.

Architecture decision: ADR 0006. Implementation is reviewable; rollout and live acceptance remain pending, so this cycle stays under work/.
- Carrier error response: return the extracted carrier message and HTTP status only; flatten carrier validation messages. Keep persisted mutation outcomes and internal form validation contracts.

2026-11-06 editable-order recovery completed:
- POST Order/UpdateOrder accepts the full CreateOrder form plus orderId; keeps identity/owner and replaces parcels atomically.
- Edits clear quotes/selection. Draft and definitively rejected shipments allow repeated quotes and explicit registration retry; preserve shipment identity and all attempts.
- Carrier acceptance with an external order ID defines shipping finality. Submitting/Unknown and previously accepted/cancelled shipments stay locked against duplicate submission.
- Concurrent pricing rejects old form snapshots; edit, selection, quote replacement and registration share order-row locks. Verified/pending payment history is retained.
- Verified 45 unit tests and 115 integration tests passed, 1 existing skip. SQL tests used an isolated database and fake carriers. No real carrier registration or payment was initiated.
- Frontend contract, ADR and endpoint QA updated. No schema migration required.

2026-11-06 order-list review: default ordering is CreationDate DESC, Id DESC; explicit client sorting remains supported with an Id tie-breaker. Current list status is still Orders.State (payment/selection); shipment lifecycle is separate and a revised customer-facing status model awaits review with the user.

2026-11-06 unified order/carrier status implementation completed:
- Added persisted Orders.OverallStatus and mapped statuses on provider status definitions, shipment parcels and history. Payment/selection State remains internal and no longer drives order-list status/filtering.
- Seeded all 13 observed ETipax statuses; preserved carrier codes/titles. General catalogue available at GET Common/GetOrderStatuses. Registration/cancellation supply ShopLink-confirmed carrier codes 59/57.
- Register/edit/select/webhook/reconciliation update the general lifecycle. Multi-parcel aggregation requires all delivered; unmapped codes retain raw data and require review. Original company status is returned by Shipment statusCode/statusTitle; operationState is technical only (legacy state retained for compatibility).
- Applied UnifiedOrderStatuses migration/backfill to local LogiwallDb: order 25 maps to general Registered=10, carrier 59; 13 ETipax mappings stored. No real carrier mutation initiated.
- Frontend shipment heading reads the actual company title; list already reads server statusName. Updated handoff/ADR/QA.
- Validation: 47 unit tests passed, 133 SQL integration tests passed with 1 existing skip, 2 React shipment tests passed; TypeScript passed and EF pending-model check clean. Restart the user's API process to load the new code; no background API host was launched.

2026-11-06 display-title change requested by user: Draft=0 now displays تکمیل نشده. IDs, mappings and lifecycle behavior are unchanged; no data migration required.

2026-11-06 frontend handoff: documented the complete GetOrderStatuses response, numeric-ID GetOrders filtering (including valid ID 0), and original carrier status in Shipment/Get. Replaced the React status text input with a catalogue-backed select. Seven order-list SQL tests passed and frontend TypeScript passed.

2026-11-06 public status simplification requested by user: only Incomplete=0 before carrier acceptance; Registered=10 also covers carrier awaiting-confirmation 61. Retired public IDs 1/2/3/4/11/23/24. Technical operation state still protects against duplicate registration. Multi-parcel orders use the outstanding parcel stage; all must be delivered for Delivered. Unknown carrier codes preserve the last valid overall stage. Applied data normalization to local LogiwallDb and updated frontend contract/ADR. 47 unit and 134 integration tests passed (1 existing skip); catalogue regression additionally verifies retired IDs are absent.

2026-11-06 contract rename requested by user: ShipmentParcelResponseDto exposes courierTrackingCode instead of trackingCode in Register/Get/Cancel. Updated frontend parcel types/rendering/tests and API handoff. Carrier request/response fields and stored TrackingCode column retain their existing names; no database migration.

2026-11-06 public provider code renamed to tipax in shipment responses and default catalogue requests. Reference lookup accepts tipax and the existing etipax alias; internal adapter/config/database identity remains stable. Frontend tracking label and handoff use tipax.

2026-11-06 backend validation audit:
- Updated 14 carrier status contract cases to assert courierStatusCode/courierStatus, including parcel fields and absence of the old title field.
- Added two regression cases: legacy accepted shipments expose initial carrier status; unknown outcomes never infer acceptance.
- Added four architecture tests for allowed Shared, Domain, Application and Infrastructure project references.
- Final results: 51 unit tests passed; 138 SQL integration tests passed; one existing placeholder skipped (no policy gap to exercise).
- Security coverage passed: ownership, OTP attempt/rate limits, admin restrictions, webhook credentials, CORS and payment verification guards.
- NuGet vulnerable-package audit reported no known vulnerable packages for the server, including transitive dependencies.
- EF reported no pending model changes. Release compilation succeeded through the test build; existing compiler warnings remain.
- Tests used an isolated SQL database and fake external providers. No real carrier registration or payment was performed.
- Known functional limits remain: SEP verification is incomplete; the Tipax no-packaging path still requires a validated carrier contract.

2026-11-06 no-carrier-packaging support completed:
- PackageTypeId=0 accepts PackagingReferenceId=null. Resolve active Tipax no-packaging records 504/521 by the selected content's packType; never invent packingId=0.
- Expose requiresPackaging on Tipax packaging references. Validate explicit flags and content compatibility before carrier calls. Freeze resolved references in quote snapshots.
- Document CreateOrder/UpdateOrder payloads, frontend filtering, field errors and mixed parcel choices. Update endpoint QA and ADR.
- Add six API regressions using the carrier route without legacy fake postal services, plus two adapter payload cases. Update existing packaging fixtures to explicitly request packaging.
- Validation: 53 unit tests and 144 SQL integration tests passed; one existing placeholder skipped. Release compilation succeeded with existing warnings. No live carrier calls or schema changes.
- This resolves the earlier no-packaging implementation limitation; SEP verification remains a separate open item.

2026-11-06 compatible reference query filters completed:
- Compared ShopLink's business-category grouping and packType resolution. Added explicit per-parcel query filters using internal Logiwall reference IDs.
- References accepts packageTypeId, packagingReferenceId (content category), and contentReferenceId (packaging category). No filters preserve the original catalogue behavior.
- Filter by active provider-scoped packing/content types; distinguish no-packaging and regular packaging. Reject invalid ranges, contradictory flags, self-category filters, unsupported categories, and foreign/inactive IDs.
- Added eight API regressions and updated the frontend handoff and endpoint QA. No database migration or external carrier request required.
- Validation: 53 unit tests and 152 SQL integration tests passed; one existing placeholder skipped. Release compilation and diff whitespace check passed.
