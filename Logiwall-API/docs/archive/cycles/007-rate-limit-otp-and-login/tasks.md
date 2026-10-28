# Cycle 007 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/007-rate-limit-otp`.

This cycle was plan 007 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: OTP attempt cap, resend cooldown, per-IP rate limits, phone validation.

| Priority | Effort | Depends on |
|---|---|---|
| P1 | M | cycle 001, cycle 005 |

## Tasks

- [x] Task 1 — Validate phone numbers at the DTO boundary
- [x] Task 2 — Cap verification attempts and add a resend cooldown
- [x] Task 3 — Per-IP rate limits on the anonymous auth endpoints
- [x] Task 4 — Tests

## Results

Status: **DONE**. Last commit on `advisor/007-rate-limit-otp`: `8621a27` (2026-10-27).

Notes from the audit status table: per-IP limits are correct only after plan 009 configures forwarded headers.
