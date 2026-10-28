# Cycle 016 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/016-net10-server`.

This cycle was plan 016 of the audit of 2026-10-27. The audit overview is `docs/archive/audit-2026-10-27.md`.

Scope, in one line: Server-side projects to .NET 10 LTS (extended on request: Shared and the clients too, Bit 8.10 → 10.7.0).

| Priority | Effort | Depends on |
|---|---|---|
| P2 | L | cycle 004, cycle 005, cycle 015 |

## Tasks

- [x] Task 1 — Feasibility spike (TFM and SDK only)
- [x] Task 2 — Bump framework packages to 10.0.x
- [x] Task 3 — Bump third-party packages that have net9/10-aware majors
- [x] Task 4 — Fix obsolete and breaking APIs
- [x] Task 5 — Full test pass and runtime check
- [x] Task 6 — Infrastructure as code

## Results

Status: **DONE**. Last commit on `advisor/016-net10-server`: `e45ff12` (2026-10-27).

Notes from the audit status table: operator: confirm `DOTNETCORE|10.0` with `az webapp list-runtimes`; `MapStaticAssets()` added, `blazor.web.js` returned 302 since the server moved to net10; MAUI retargeted but not built (no MAUI workload); SSH.NET High removed with `AspNetCore.HealthChecks.Network`; KubernetesClient Moderate remains via `AspNetCore.HealthChecks.UI`.
