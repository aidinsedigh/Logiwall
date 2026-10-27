# Cycle 017 — tasks

Read this file first when you resume the cycle. Then read `plan.md` in full, and `spec.md` by
section. Branch: `advisor/017-persian-messages-and-logs` (create it before Task 1, as
`plan.md` "Git workflow" says).

Scope, in one line each:

* Every client message is Persian (ADR-0003, written in Task 1).
* Every log entry has the structured format and a correlation ID (ADR-0004, written in Task 1).
* 19 open items from cycle 1 and the .NET 10 migration (`spec.md` §7).

## Tasks

- [x] Task 1 — ADR-0003, ADR-0004, conventions, `instruction.md` §17 (FR-D1-FR-D4)
- [x] Task 2 — `fa-IR` for every request; complete Persian resources; completeness test (FR-M2, FR-M4)
- [x] Task 3 — Client messages from resource keys; no provider text (FR-M1, FR-M3, FR-M5, FR-M9, FR-O13)
- [x] Task 4 — Persian binding, JSON, 429 and unknown-error messages (FR-M6-FR-M8)
- [x] Task 5 — Correlation ID middleware; Serilog from `appsettings.json` (FR-L2-FR-L4, FR-L6, FR-L8)
- [x] Task 6 — `ServerExceptionHandler` logs each handled exception once (FR-L5)
- [x] Task 7 — Refactor the 20 log calls to the template (FR-L1, FR-L7)
- [x] Task 8 — Account delete archives the user (FR-O1)
- [x] Task 9 — Atomic OTP attempts; SMS failure; login key; code range (FR-O2, FR-O4, FR-O5, FR-O14)
- [x] Task 10 — `OrderDto.Status`; parallel re-quote lock (FR-O3, FR-O15)
- [ ] Task 11 — Used national code; all pending payments; SEP split test (FR-O6-FR-O8)
- [ ] Task 12 — One token signer; new development certificate (FR-O9, FR-O10)
- [ ] Task 13 — Dead code, `KnownIPNetworks`, remove health-check UI (FR-O11, FR-O12, FR-O16, FR-O17)
- [ ] Task 14 — Client loading overlay and unresolved Bit tags (FR-O18, FR-O19)
- [ ] Task 15 — Final check, self-review, archive the cycle

## Not in this cycle

* Plan 012 Phase B (SEP verify): stays BLOCKED in `docs/work/001-audit-remediation/tasks.md`.
* Operator actions and product decisions in cycle 1's `tasks.md`.
* IaC, the MAUI build, and the old client CSS variables (`spec.md` §2).

## Results

(Task 15 writes the final counts and the self-review here.)
