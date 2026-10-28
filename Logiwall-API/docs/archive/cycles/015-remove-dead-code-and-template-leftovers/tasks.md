# Cycle 015 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/015-remove-dead-code`.

This cycle was plan 015 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Remove unused packages, OData, dead middleware, commented code; namespace rename.

| Priority | Effort | Depends on |
|---|---|---|
| P3 | S–M | cycle 005, cycle 008, cycle 012 (run after cycle 001–cycle 014) |

## Tasks

- [x] Task 1 — Remove unused packages
- [x] Task 2 — Remove the OData pipeline
- [x] Task 3 — Delete dead and dangerous code
- [x] Task 4 — Namespace rename

## Results

Status: **DONE**. Last commit on `advisor/015-remove-dead-code`: `a67d010` (2026-10-27).

Notes from the audit status table: 24 migrations before and after; `src/Client` untouched.
