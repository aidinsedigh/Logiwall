# Cycle 010 — The currency unit of every amount from parcel value to gateway charge is documented, with a verdict on whether customers are charged correctly: spec

## Contents

- L19 Summary
- L28 Why this matters
- L46 Repo facts you need
- L51 Current state (the money path)
- L71 Scope
- L77 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 010 (investigation) of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

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

## Scope

**In scope**: `docs/adr/0001-currency-units.md` (create) and the this cycle's `tasks.md` status row.

**Out of scope**: any code change, including "obvious" fixes. Plan 012 owns the fix.

## Maintenance notes

- Plan 012 must read this record before it implements amount comparison.
- When the provider answers the questions, update the record's verdict and the README status.
