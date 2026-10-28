> Detail for `instruction.md` §8. Load when you throw, catch or map an exception, or write an
> error message for the client.

# 8. Error Handling

Logiwall has two error paths. Both exist in the code now, and the front end depends on both.

| Path | Use it for | Result for the client |
|---|---|---|
| `ServiceResponse<T>` with `Succuss = false` | An expected business failure that the service reports (for example: no customer for this user) | HTTP 400 with the `ServiceResponse<T>` body |
| Throw a `KnownException` subclass | A failure that stops the request: not found, forbidden, conflict, validation | `ServerExceptionHandler` maps it to `RestErrorInfo` and the status code of the `RestException` |

## 8.1 Rules

* Follow the path the service you edit already uses. Do not mix both paths in one method.
* Do not introduce a new `Result<T>` type. `ServiceResponse<T>` is the result type.
* Throw only the existing subclasses in `src/Logiwall.Shared/Exceptions/`:
  `BadRequestException`, `ResourceNotFoundException`, `ForbiddenException`,
  `ConflictException`, `ResourceValidationException`, `UnauthorizedException`,
  `TooManyRequestsExceptions`, `DomainLogicException`, `ServerConnectionException`.
  Add a new subclass only when no existing one fits.
* Pass a resource key (`nameof(AppStrings.X)`) as the message. `ServerExceptionHandler` then
  localizes it. Do not hard-code client-facing text.
* Never return `ex.Message` to the client. It can leak internal detail (plan 009).
  `ServerExceptionHandler` returns the `UnknownException` message for an unknown exception in
  every environment.
* Do not catch `Exception` only to turn it into `Succuss = false`. Let unknown exceptions reach
  `ServerExceptionHandler`, so they are logged with the request ID.
* Catch an exception only when you can add context or recover. Keep the inner exception.
* Do not throw `ArgumentNullException()` or `NotImplementedException` on a path that a client
  can reach.

### Client messages are Persian (ADR-0003)

`docs/adr/0003-persian-client-messages.md` records this decision.

* Every client message is Persian: `ServiceResponse<T>.Message`, `PaymentResponseDto.Message`,
  `RestErrorInfo.Message` and each message in `RestErrorInfo.Payload`.
* The server culture is `fa-IR` for every request. `Accept-Language` does not change it.
* C# code in `Logiwall.Server` and `Logiwall.Infrastructure` contains no client-message literal.
  Inject `IStringLocalizer<AppStrings>` through the constructor and use
  `localizer[nameof(AppStrings.X)]`. Do not use the static `AppStrings.X` properties.
* A new key needs an entry in `AppStrings.resx` (English) and `AppStrings.fa.resx` (Persian).
  `PersianResourceTests` fails when the Persian entry is missing.
* Never pass provider text (SEP `Title`/`Message`, MyTipax, SMS) to the client. Log it, and
  return your own resource key.
* Model-binding, JSON-format and rate-limit (429) errors also return Persian messages.
* Log templates and exception text that never reach a client stay English.

## 8.2 Where the mapping is

`src/Logiwall.Server/Services/ServerExceptionHandler.cs` adds the `Request-Id` header, unwraps
the exception and writes a `RestErrorInfo` with `Key`, `Message` and `ExceptionType`.
