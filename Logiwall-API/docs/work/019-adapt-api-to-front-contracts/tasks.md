# Cycle 019 — tasks

Read this file first when you resume the cycle. Then read `spec.md`. Branch: `advisor/cycles-001-017`.

Scope: fix the backend bugs and contract mismatches in the 2026-10-28 E2E report. The front does
not change; the API matches it.

## Tasks

- [x] Task 1 — identity and profile (D1-D5)
- [x] Task 2 — order quote, selection and payment (D6-D10, D14)
- [x] Task 3 — order list (D11-D13)
- [x] Task 4 — QA files, build, tests

## Results (2026-10-28)

* Build: `Logiwall.Server.csproj` 0 errors (built to a scratch folder: a running `Logiwall.Server` locks `bin`).
* Unit tests: 29/29, incl. 9 `PickupSlotsTests`.
* Integration tests: 80 passed, 1 skipped (old skip). New: `ProfileTests` (6), quote shape, pickup
  validation, `SelectPaymentType` (link, cash on delivery, unchosen option, repeated reference),
  order list total count, names and amount.
* Not run: the React app against the new API. Not verified: real MyTipax amounts (D7) and real SEP.
* Open: questions Q1-Q5 and items N1-N6 in `spec.md`. The cycle stays in `work/` until they are decided.
