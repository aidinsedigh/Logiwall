# Cycle 003 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/003-remove-committed-secrets`.

This cycle was plan 003 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Credentials out of source, migrations and logs; config-based options.

| Priority | Effort | Depends on |
|---|---|---|
| P1 | M | — |

## Tasks

- [x] Task 1 — Add options classes and bind them
- [x] Task 2 — Providers read credentials from options
- [x] Task 3 — Remove credential properties from `AppSettingsDto`
- [x] Task 4 — Strip secrets and dead code from `SMSService`
- [x] Task 5 — Stop logging credentials in `HttpProvider`
- [x] Task 6 — Blank the seeded values and delete the rows
- [x] Task 7 — Configuration skeleton, connection string, gitignore and log file

## Results

Status: **DONE**. Last commit on `advisor/003-remove-committed-secrets`: `ef4a7c1` (2026-10-27).

Notes from the audit status table: operator: rotate credentials, set `Providers:*` and `Sms:PersiaFava:*` incl. the SMS `Url`.
