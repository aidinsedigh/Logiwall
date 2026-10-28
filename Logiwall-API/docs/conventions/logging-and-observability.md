> Detail for `instruction.md` §10. Load when you add or change a log statement, or any
> health-check, tracing or monitoring concern.

# 10. Logging and Observability

## 10.1 Logging

Logiwall uses **Serilog**. `docs/adr/0004-structured-log-format.md` records the format below.
Serilog reads its levels and sinks from the `Serilog` section of
`src/Logiwall.Server/appsettings.json` (a daily rolling file in `src/Logiwall.Server/Logs/`).
`Program.cs` keeps a console bootstrap logger for start-up and fatal errors.
`Program.Middlewares.cs` adds `UseSerilogRequestLogging()` with a template in this format.

* Inject `ILogger<T>` through the constructor. Do not add calls to the static `Serilog.Log`
  class outside `Program.cs`.
* Use message templates with named properties. Do not build log text with string interpolation.

Use this format for each log message. Every log call in `Logiwall.Server` and
`Logiwall.Infrastructure` uses it:

```text
[ClassName][MethodName][{Field1}][{Field2}] Sentence.
```

* `MethodName` is the method name without `Async`. For a private helper, use the public method
  that calls it when the helper name says nothing to an operator.
* The sentence is English and ends with a full stop.
* Pass an exception as the first argument: `logger.LogWarning(ex, "...")`. Never put
  `ex.Message` in the template.
* A request-independent entry uses the same form: `[Program][Main] Web host started.`

Example:

```text
[Order][CreateOrder][{OrderId}][{UserId}] Order created.
```

Use the fields that a reader needs to understand the event from one log entry: `OrderId`,
`UserId`, `CustomerId`, `PaymentId`, `OrderState`, `PaymentStatus`, `ProviderType`, the HTTP
status code. Include only the useful fields.

### ClassName

Remove technical suffixes: `OrderService` becomes `[Order]`, `MyTipaxProvider` becomes
`[MyTipax]`, `AuthenticationService` becomes `[Authentication]`. The log describes the business
operation, not the mechanism.

### Never log

* passwords, OTP codes, tokens, API keys or other secrets
* HTTP header values
* request or response bodies of external providers (SEP, MyTipax, SMS)
* phone numbers or other personal data, unless masked

Plan 003 removed the body and header logging. Do not add it back.

### Correlation ID

`CorrelationIdMiddleware` runs first in the pipeline. It reads `X-Correlation-Id` from the
request when the value has 1-64 ASCII letters, digits, `-` or `_`; else it makes a GUID. It sets
`HttpContext.TraceIdentifier`, the `X-Correlation-Id` response header and the `CorrelationId`
log property. `ServerExceptionHandler` writes the same value to `Request-Id`. The file sink
writes it on each line:

```text
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}
```

### Handled exceptions

`ServerExceptionHandler` logs each exception it handles exactly once: a known exception at
`Warning`, an unknown exception at `Error` with the exception object. Do not log an exception
again before you let it reach the handler.

## 10.2 Observability

The server has health checks (`AspNetCore.HealthChecks.*` packages) and Application Insights in
the Azure stack (`src/Logiwall.Iac/`). Consider:

* structured logging
* the `Request-Id` header that `ServerExceptionHandler` adds
* health checks for the database and external providers
* metrics and tracing

Do not add observability infrastructure without a clear operational purpose.
