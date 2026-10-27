> Detail for `instruction.md` §10. Load when you add or change a log statement, or any
> health-check, tracing or monitoring concern.

# 10. Logging and Observability

## 10.1 Logging

Logiwall uses **Serilog**. `src/Logiwall.Server/Program.cs` configures it with a daily rolling
file in `src/Logiwall.Server/Logs/`, and `Program.Middlewares.cs` adds
`UseSerilogRequestLogging()`.

* In new code, inject `ILogger<T>` through the constructor. Do not add new calls to the static
  `Serilog.Log` class. Plan 013 converts the existing calls in `HttpProvider`.
* Use message templates with named properties. Do not build log text with string interpolation.

Use this format for each log message:

```text
[ClassName][MethodName][{Field1}][{Field2}] Message
```

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

Plan 003 removes the existing body and header logging. Do not add it back.

## 10.2 Observability

The server has health checks (`AspNetCore.HealthChecks.*` packages) and Application Insights in
the Azure stack (`src/Logiwall.Iac/`). Consider:

* structured logging
* the `Request-Id` header that `ServerExceptionHandler` adds
* health checks for the database and external providers
* metrics and tracing

Do not add observability infrastructure without a clear operational purpose.
