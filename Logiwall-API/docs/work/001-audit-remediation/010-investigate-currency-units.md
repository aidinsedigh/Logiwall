# Plan 010 (investigation): The currency unit of every amount from parcel value to gateway charge is documented, with a verdict on whether customers are charged correctly

> **Executor instructions**: This is an **investigation** plan. You write one
> document and change **no code**. Follow the steps, run every verification
> command, and if anything in the "STOP conditions" section occurs, stop and
> report. When done, update the status row for this plan in `docs/work/001-audit-remediation/tasks.md`
> — unless a reviewer dispatched you and told you they maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs Logiwall-API/src/Logiwall.Infrastructure/Providers/Sep724Provider.cs`
> Plans 002/003/009 change other lines in these files. Locate the excerpts below
> by content. If an excerpt's arithmetic (the `/10`, the `(long)` cast or the
> share formula) has changed, STOP: someone has already acted on this.

## Status

- **Priority**: P1 (it gates plan 012; this is money)
- **Effort**: S
- **Risk**: LOW (no code changes)
- **Depends on**: none
- **Category**: bug (investigation)
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Iran uses two units: the **rial** (official) and the **toman** (= 10 rials, used
colloquially and by many APIs). The code converts in one place and not in
another:
- The parcel's declared value is **divided by 10** before it is sent to the
  MyTipax courier API, which suggests rial → toman.
- The price MyTipax returns (`FinalAmount`) is stored, and later sent to the
  SEP payment gateway **without any conversion**, after being truncated with
  `(long)`.

If MyTipax quotes in toman and SEP charges in rial, every customer pays **one
tenth** of the price. If both are rial, the `/10` under-declares the parcel
value for insurance. The commission split between the two bank accounts also
uses integer division. Plan 012 (server-side payment verification) compares the
gateway amount against the stored amount, and it cannot do that safely until
the units are known.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. It contains `Logiwall-API/` (backend) and `Logiwall-Front/` (React UI; read-only for you).
- Runtime settings such as `LogiwallShare`, `MyTipaxShare` and the IBAN keys are rows in the DB table `AppSettings`, seeded in `src/Logiwall.Infrastructure/Data/Migrations/20240924213908_AddAccountInfoInAppSettings.cs`. **IBAN values are sensitive; don't copy them into the document.** Share percentages may be quoted.

## Current state (the money path)

1. `src/Logiwall.Shared/Dtos/OrderItems/CreateOrderRequestDto.cs`: parcels carry `ParcelValue` (decimal) from the client.
2. `src/Logiwall.Infrastructure/Services/OrderItems/OrderService.cs`, in `GetOptionsAsync`:
   `Value = (t.ParcelValue/10).ToString(),` goes into the MyTipax inquiry.
3. Same method: the MyTipax response is parsed with
   `FinalAmount = decimal.Parse(t.FinalAmount, CultureInfo.InvariantCulture)`
   (and similarly `ServiceAmount`, `Tax`, `PackagingAmount`), then stored on `OrderInqueryOption`.
4. Same file, `SelectOptionAndPaymentAsync`: `Amount = orderInquiry.FinalAmount` is passed to `ISep724Provider.GetPaymentTokenAsync`.
5. `src/Logiwall.Infrastructure/Providers/Sep724Provider.cs`, in `GetPaymentTokenAsync`:
   ```csharp
   var amount = (long)requestDto.Amount;
   var logiwallShare = amount * int.Parse(appSettings.LogiwallShare) / 100;
   ...
   new(){ Amount = logiwallShare, Iban = appSettings.LogiwallIban },
   new(){ Amount = amount - logiwallShare, Iban = appSettings.MyTipaxIban}
   ```
   `MyTipaxShare` is seeded but never read (`grep -rn MyTipaxShare src --include=*.cs`).
6. `src/Logiwall.Shared/Dtos/Providers/PostalService/` holds the MyTipax DTOs (field names, possibly with comments).

## Steps

### Step 1: Gather code and UI evidence

- Read the DTOs in step 6 and any XML comments or `[Display]` attributes that mention units.
- In `Logiwall-Front/src`, search for unit labels near price or amount display:
  `grep -rn "تومان\|ریال\|toman\|rial" Logiwall-Front/src`. Record which fields the UI labels as which unit, and whether it divides or multiplies anything (`grep -rn "/ 10\|\* 10" Logiwall-Front/src`).
- Search the backend for other conversions: `grep -rnE "/ ?10\b|\* ?10\b" Logiwall-API/src --include=*.cs | grep -v Migrations`.

### Step 2: Gather runtime evidence (if available locally)

If `Logiwall-API/src/Logiwall.Server/Logs/*.txt` files exist locally, look for
MyTipax inquiry responses and SEP token requests, and compare **magnitudes
only**. For example: "a domestic parcel quote is around N digits". **Do not
copy log lines, phone numbers, tokens or headers into the document.** If there
are no logs, write "no runtime evidence available".

### Step 3: Write the decision record

Create `docs/adr/0001-currency-units.md` (create the folder) with these sections:
1. **Context**: the 5-step money path above.
2. **Evidence**: from Steps 1–2, each item with a `file:line` reference.
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
- `git status` → only that file was added (plus the `docs/work/001-audit-remediation/tasks.md` status update).

## Scope

**In scope**: `docs/adr/0001-currency-units.md` (create) and the `docs/work/001-audit-remediation/tasks.md` status row.

**Out of scope**: any code change, including "obvious" fixes. Plan 012 owns the fix.

## Test plan

None (no code). Plan 012 turns the verdict into unit tests for the conversion and the split.

## Done criteria

- [ ] The decision record exists with all 6 sections
- [ ] The verdict is exactly one of (a), (b) or (c)
- [ ] No IBANs, phone numbers, tokens or log lines in the document
- [ ] No source files changed
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated, with the verdict letter in the Status column (e.g. `DONE (verdict c)`)

## STOP conditions

- Evidence shows **money already moved incorrectly in production**, for example
  logs showing SEP amounts that are 10× too small for the quoted service. Stop
  and report immediately: this is an incident, not a plan item.
- You would need to call a live provider API to answer a question. Don't; list it under "Questions" instead.

## Maintenance notes

- Plan 012 must read this record before it implements amount comparison.
- When the provider answers the questions, update the record's verdict and the README status.
