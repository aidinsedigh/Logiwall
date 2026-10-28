# Cycle 011 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/011-order-quote-flow`.

This cycle was plan 011 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Idempotent quotes, DI-resolved providers, order state and history, transactions.

| Priority | Effort | Depends on |
|---|---|---|
| P2 | M | cycle 002, cycle 005, cycle 009 |

## Tasks

- [x] Task 1 — Extend `OrderState`
- [x] Task 2 — Resolve postal providers through DI
- [x] Task 3 — Rewrite `GetOptionsAsync` to be idempotent and provider-isolated
- [x] Task 4 — Record the selection in `SelectOptionAndPaymentAsync`
- [x] Task 5 — Transactional registration and profile update
- [x] Task 6 — Fake provider and tests

## Results

Status: **DONE**. Last commit on `advisor/011-order-quote-flow`: `040600b` (2026-10-27).

Notes from the audit status table: Register rollback test skipped: no password-policy gap; `IPostalServiceBase.Payment` stub kept, see 015.
