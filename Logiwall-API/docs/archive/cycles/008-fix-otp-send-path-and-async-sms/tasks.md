# Cycle 008 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/008-otp-send-path`.

This cycle was plan 008 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Async SMS with timeout, correct OTP ordering, logging, disabled accounts refused.

| Priority | Effort | Depends on |
|---|---|---|
| P2 | M | cycle 003, cycle 005, cycle 007 |

## Tasks

- [x] Task 1 — Async SMS contract and implementation
- [x] Task 2 — Reorder and harden `SendOtpCodeAsync`
- [x] Task 3 — Refuse disabled accounts at login
- [x] Task 4 — Update the fake and add tests

## Results

Status: **DONE**. Last commit on `advisor/008-otp-send-path`: `5dc3146` (2026-10-27).

Notes from the audit status table: product decision to confirm: archived or inactive accounts cannot log in.
