# Cycle 018 — tasks

Read this file first when you resume the cycle. Then read `plan.md`. Branch: `advisor/cycles-001-017`.

Scope: the Blazor client calls only routes the server maps. The client is the admin panel
(ADR-0005): password sign-in for admins, `AdminsOnly` on its pages and endpoints, business-wide
order counts on the dashboard (`spec.md`).

## Tasks

- [x] Task 1 — sign-in through `GetToken`, delete the password pages, show `ServiceResponse` messages (D1, D2, D7, D8, D11)
- [x] Task 2 — order stats on the dashboard, delete the product widgets (D3, D4, D5, D10)
- [x] Task 3 — remove the attachment calls and `IMinimalApiController` (D6, D9)
- [x] D12, D13 — server-side `HttpClient` base address, `LoginProvider` on sign-in (found in the live run)
- [x] Task 4 — admin panel: `AdminsOnly`, `SeedAdminRole` migration, global stats, ADR-0005 (D14)
- [x] Verification — build, tests, live run in the browser

## Results (2026-10-28, after Task 4)

* Build: `Logiwall.IntegrationTests.csproj` (8 projects) 0 errors, no new warnings in the changed files (built to a scratch folder; the running server locks `bin`).
* Tests: unit 20/20; integration 64 passed, 1 skipped (old skip), incl. 4 `DashboardStatsTests`. The tests apply `SeedAdminRole` through `MigrateAsync`.
* Live (port 5031, E2E database, fake SMS): the operator SQL from ADR-0005 gave `Admin` to one user (`RolesGiven = 1`). A customer with a correct password sees "دسترسی به این منبع مجاز نیست." and stays signed out. A wrong password shows "نام کاربری یا رمز عبور صحیح نیست". The admin opens `/dashboard` (2, 2, 0, the same as all orders in the database) and `/edit-profile`. Every `/api` call returns 200, except the expected 401 for the wrong password.
* Found: `sqlcmd` needs `SET QUOTED_IDENTIFIER ON` for an insert into `Roles` (filtered index). The ADR SQL has it; EF migrations set it themselves.
* Not committed. The cycle stays in `work/` until the user accepts it.
