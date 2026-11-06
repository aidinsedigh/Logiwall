# QA: /api/Shipment/Register

The quote's shared PaymentMethod determines settlement, independently of ETipax env settings.
Online (71) requires verified sufficient payment. All carrier-settled methods (72-78) require an unexpired quote and no pending online-payment workflow.
The adapter must confirm that the frozen request matches the shared method before external submission.

Carrier validation is saved as a rejected attempt before its message/status response is returned.
Carrier validation messages are flattened into a single message. Transport ambiguity remains 409/Unknown. A definitive rejection allows explicit retry, with or without editing and re-quoting; preserve the same shipment and append an attempt. Accepted and cancelled shipments cannot be replaced by editing.

## Acceptance criteria

Only the order owner can register a selected quote. Online shipping requires verified sufficient payment; carrier collection requires explicit matching server policy.

## Positive and edge cases

Parallel requests create one shipment and one carrier call. Repeat after success returns the same tracking codes.

## Negative, authorization and failure cases

Reject missing/expired quotes and unverified payment before carrier I/O. Save timeout/5xx/invalid success response as Unknown. Restart cannot resend it.

## Integration

Use the fake IShippingCarrier and a real isolated SQL Server database. No live shipping calls. Check ServiceResponse wire casing, Persian errors and absence of raw payloads in responses/logs. Unit/provider tests and Orders/ShipmentTests cover the automated cases. See cycle 020 setup.md for manual bootstrap and mode configuration.

