# Cycle 001 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/001-remove-shared-default-password`.

This cycle was plan 001 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: OTP-created accounts have no password; existing shared default passwords cleared.

| Priority | Effort | Depends on |
|---|---|---|
| P1 | S | — |

## Tasks

- [x] Task 1 — Create OTP users without a password and check the result
- [x] Task 2 — Add a data migration that clears the shared password from unregistered users

## Results

Status: **DONE**. Last commit on `advisor/001-remove-shared-default-password`: `8818d1a` (2026-10-27).

Notes from the audit status table: migration `ClearSharedDefaultPasswords`; manual DB check skipped, no local SQL Server.
