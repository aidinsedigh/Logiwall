# Cycle 009 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/009-harden-production-defaults`.

This cycle was plan 009 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Swagger flag, sample endpoint and SignUp removed, CORS/forwarded headers configurable, no `ex.Message` leaks.

| Priority | Effort | Depends on |
|---|---|---|
| P2 | S | cycle 005 |

## Tasks

- [x] Task 1 — Swagger only where enabled, and remove the sample endpoint
- [x] Task 2 — Configurable CORS
- [x] Task 3 — Explicit forwarded-header trust
- [x] Task 4 — Delete the account-deleting `SignUp` action
- [x] Task 5 — Stop returning exception text
- [x] Task 6 — Tests

## Results

Status: **DONE**. Last commit on `advisor/009-harden-production-defaults`: `df211af` (2026-10-27).

Notes from the audit status table: operator: set `Cors__AllowedOrigins__0`, `ForwardedHeaders__TrustAllProxies=true` on App Service, `AllowedHosts`.
