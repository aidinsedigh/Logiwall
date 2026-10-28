# Cycle 005 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/005-test-and-ci-baseline`.

This cycle was plan 005 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: xUnit + Testcontainers SQL Server harness, regression tests, GitHub Actions CI.

| Priority | Effort | Depends on |
|---|---|---|
| P1 | M | cycle 001, cycle 002, cycle 004 |

## Tasks

- [x] Task 1 — Make the host testable (two production one-liners)
- [x] Task 2 — Create the unit test project
- [x] Task 3 — Create the integration test project and harness
- [x] Task 4 — Smoke test
- [x] Task 5 — Regression tests for plans 001 and 002
- [x] Task 6 — Server-only solution filter and CI

## Results

Status: **DONE**. Last commit on `advisor/005-test-and-ci-baseline`: `cbb2338` (2026-10-27).

Notes from the audit status table: 4 unit + 8 integration tests pass locally; finding: committed dev `IdentityCertificate.pfx` expired 2025-01, tests generate their own.
