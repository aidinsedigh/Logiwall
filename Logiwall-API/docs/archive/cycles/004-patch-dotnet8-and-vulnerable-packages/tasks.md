# Cycle 004 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/004-patch-dotnet8`.

This cycle was plan 004 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: SDK roll-forward and 8.0.x patch bumps; no High/Critical advisories.

| Priority | Effort | Depends on |
|---|---|---|
| P1 | S | — |

## Tasks

- [x] Task 1 — Let the SDK roll forward within .NET 8
- [x] Task 2 — Remove the redundant System.Text.Json pin
- [x] Task 3 — Bump Microsoft packages to the latest 8.0 patch
- [x] Task 4 — Clear the remaining transitive advisories

## Results

Status: **DONE**. Last commit on `advisor/004-patch-dotnet8`: `200f34c` (2026-10-27).

Notes from the audit status table: 8.0.33; exception: transitive SSH.NET 2023.0.0 High via `AspNetCore.HealthChecks.Network`, fix only in 2026.0.0 major; KubernetesClient Moderate.
