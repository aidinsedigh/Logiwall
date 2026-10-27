# ADR-0003: Client messages are Persian

**Status:** Accepted, 2026-10-27

## Decision

Every client message is Persian. A client message is text in a response body that a client can
show to a person: `ServiceResponse<T>.Message`, `PaymentResponseDto.Message`,
`RestErrorInfo.Message` and each message in `RestErrorInfo.Payload`.

* Each client message comes from a resource key in `AppStrings` or `IdentityStrings`
  (`src/Logiwall.Shared/Resources/`). Each key has a Persian resource in the `.fa.resx` file.
* The server uses culture `fa-IR` for every request. It ignores `Accept-Language`, the culture
  cookie and the `culture` query string.
* Text from an external provider (SEP, MyTipax, SMS) never becomes a client message. The server
  logs it and returns its own resource key.
* Log templates and exception text that never reach a client stay English.

## Context

The React app (`Logiwall-Front`) shows `message` from every error body to the user in a toast.
Before this decision the server returned English text, Persian text and provider text mixed.
The users read Persian.

## Consequences

* A new client message needs two resx entries: the neutral English text and the Persian text.
  A unit test (`PersianResourceTests`) fails when a Persian resource is missing.
* The neutral `.resx` files stay as the fallback and the English source text.
* An English response needs a new decision that adds cultures back to the request
  localization options in `Program.Middlewares.cs`.
