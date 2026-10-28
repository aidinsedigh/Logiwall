# Cycle 010 — plan

## Contents

- L25 STOP conditions
- L32 Tasks
  - L34 Task 1: Gather code and UI evidence
  - L41 Task 2: Gather runtime evidence (if available locally)
  - L49 Task 3: Write the decision record
- L67 Test plan
- L71 Done criteria

> **Executor instructions**: This is an **investigation** plan. You write one
> document and change **no code**. Follow the steps, run every verification
> command, and if anything in the "STOP conditions" section occurs, stop and
> report. When done, update the status row for this plan in this cycle's `tasks.md`
> — unless a reviewer dispatched you and told you they maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs Logiwall-API/src/Logiwall.Infrastructure/Providers/Sep724Provider.cs`
> Plans 002/003/009 change other lines in these files. Locate the excerpts below
> by content. If an excerpt's arithmetic (the `/10`, the `(long)` cast or the
> share formula) has changed, STOP: someone has already acted on this.

## STOP conditions

- Evidence shows **money already moved incorrectly in production**, for example
  logs showing SEP amounts that are 10× too small for the quoted service. Stop
  and report immediately: this is an incident, not a plan item.
- You would need to call a live provider API to answer a question. Don't; list it under "Questions" instead.

## Tasks

### Task 1: Gather code and UI evidence

- Read the DTOs in task 6 and any XML comments or `[Display]` attributes that mention units.
- In `Logiwall-Front/src`, search for unit labels near price or amount display:
  `grep -rn "تومان\|ریال\|toman\|rial" Logiwall-Front/src`. Record which fields the UI labels as which unit, and whether it divides or multiplies anything (`grep -rn "/ 10\|\* 10" Logiwall-Front/src`).
- Search the backend for other conversions: `grep -rnE "/ ?10\b|\* ?10\b" Logiwall-API/src --include=*.cs | grep -v Migrations`.

### Task 2: Gather runtime evidence (if available locally)

If `Logiwall-API/src/Logiwall.Server/Logs/*.txt` files exist locally, look for
MyTipax inquiry responses and SEP token requests, and compare **magnitudes
only**. For example: "a domestic parcel quote is around N digits". **Do not
copy log lines, phone numbers, tokens or headers into the document.** If there
are no logs, write "no runtime evidence available".

### Task 3: Write the decision record

Create `docs/adr/0001-currency-units.md` (create the folder) with these sections:
1. **Context**: the 5-step money path above.
2. **Evidence**: from Tasks 1–2, each item with a `file:line` reference.
3. **Unit per field**: a table with columns *Field | Unit | Confidence (confirmed / inferred / unknown) | Evidence*. Fields: `ParcelValue`, the MyTipax inquiry `Value`, MyTipax `FinalAmount`/`ServiceAmount`/`Tax`/`PackageService`, the SEP `Amount`, the SEP account split amounts.
4. **Verdict**: one of
   *(a) consistent: no change needed;*
   *(b) mismatch confirmed: the SEP amount must be converted by ×10 (or ÷10), and the fix belongs in plan 012;*
   *(c) unknown: provider confirmation required.*
5. **Questions for the operator/provider**: concrete yes/no questions. For example: "Does the MyTipax `/api/v1/orders/inquiry` response express `finalAmount` in rials or tomans?", "Does SEP `token` accept `amount` in rials?", "Should the account split use `MyTipaxShare` or the remainder of `LogiwallShare`?", "Is rounding (instead of truncation) acceptable to the gateway?".
6. **Split rule**: document the current formula. Say that integer division truncates, that `MyTipaxShare` is ignored, and give a worked example with a made-up amount.

**Verify**:
- `ls docs/adr/0001-currency-units.md` → exists.
- `grep -nE "IR[0-9]{2}" docs/adr/0001-currency-units.md` → no output (no IBANs).
- `git status` → only that file was added (plus the this cycle's `tasks.md` status update).

## Test plan

None (no code). Plan 012 turns the verdict into unit tests for the conversion and the split.

## Done criteria

- [ ] The decision record exists with all 6 sections
- [ ] The verdict is exactly one of (a), (b) or (c)
- [ ] No IBANs, phone numbers, tokens or log lines in the document
- [ ] No source files changed
- [ ] this cycle's `tasks.md` status row updated, with the verdict letter in the Status column (e.g. `DONE (verdict c)`)
