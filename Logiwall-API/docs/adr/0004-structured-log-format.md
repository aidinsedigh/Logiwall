# ADR-0004: Structured log format and correlation ID

**Status:** Accepted, 2026-10-27

## Decision

Every log template has this form:

```text
[Class][Method][{Field1}]...[{FieldN}] Sentence.
```

* `Class` is the class name without `Service`, `Provider`, `Controller`, `Handler` or
  `Middleware`. `Method` is the method name without `Async`.
* Each `{Field}` is a named property in PascalCase. The sentence is English and ends with a
  full stop. No string interpolation. An exception is the first argument, never `ex.Message`.
* Each HTTP request has one correlation ID. The server reads it from the `X-Correlation-Id`
  request header when the value is safe, else it makes a GUID. The ID is in the
  `X-Correlation-Id` response header, in the `Request-Id` header of an error response and in the
  `CorrelationId` property of each log entry.
* Serilog reads its levels and sinks from the `Serilog` section of `appsettings.json`.
* `ServerExceptionHandler` logs each exception it handles exactly once: a known exception at
  `Warning`, an unknown exception at `Error` with the exception object.

## Context

Before this decision the log had free-form English sentences and no link between the entries
of one request. An operator could not find all entries of one failed request.

## Consequences

* Every new log call follows the template. A review rejects other forms.
* An operator finds all entries of one request with one search on `CorrelationId`.
* A client can send `X-Correlation-Id` to link its own logs to the server log.
