# Cycle 012 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/012-verify-payments`.

This cycle was plan 012 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Server-side payment verification (Phase A: pending/no client trust; Phase B: SEP verify).

| Priority | Effort | Depends on |
|---|---|---|
| P1 | L | cycle 002, cycle 005, cycle 010, cycle 011 |

## Tasks

- [x] Task A1 — Model
- [x] Task A2 — Record a pending payment when the link is issued
- [x] Task A3 — Remove client-reported payment status
- [x] Task A4 — Phase A tests
- [ ] Task B0 — Gate (BLOCKED)
- [ ] Task B1 — Implement `GetPaymentInfoAsync` (BLOCKED)
- [ ] Task B2 — Implement `VerifyWithGatewayAsync` (BLOCKED)
- [ ] Task B3 — Phase B tests (BLOCKED)

## Results

Status: **BLOCKED**. Last commit on `advisor/012-verify-payments`: `fc4ecd9` (2026-10-27).

Notes from the audit status table: Phase A done; Phase B needs SEP verify API docs and the ADR-0001 unit answer.
