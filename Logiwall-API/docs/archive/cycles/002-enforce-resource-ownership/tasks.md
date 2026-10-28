# Cycle 002 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/002-enforce-resource-ownership`.

This cycle was plan 002 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Contacts, orders, options and payments resolve only for their owner (IDOR).

| Priority | Effort | Depends on |
|---|---|---|
| P1 | S | — |

## Tasks

- [x] Task 1 — Scope customer-contact read and update to the caller
- [x] Task 2 — Scope order options and option selection to the caller
- [x] Task 3 — Scope payment lookups to the caller (ownership only)

## Results

Status: **DONE**. Last commit on `advisor/002-enforce-resource-ownership`: `a24ce53` (2026-10-27).
