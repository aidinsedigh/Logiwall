# Cycle 018 — spec: fix the Blazor client's API calls

## Problem

The Blazor panel (`src/Client/Logiwall.Client.Core`) calls routes the server does not have.
The 2026-10-28 review found these defects:

| # | Defect | Source |
|---|---|---|
| D1 | Sign-in posts to `api/Identity/SignIn`. The route does not exist (405). | `IIdentityController.cs:27` |
| D2 | Sign-in uses a password. OTP users have no password. The field is type email, but `UserName` is a phone number. | `SignInPage.razor:21-24`, `AuthenticationService.cs:52` |
| D3 | Two dashboard widgets call `GetProductsCountPerCategoryStats` and `GetProductsSalesStats`. The routes do not exist (302 to `/not-found`). | `IDashboardController.cs` |
| D4 | The stats widget shows product and category counts. Logiwall has no products. The endpoint always returns zeros. | `DashboardController.cs` |
| D5 | The percentage widget calls nothing. | `ProductsPercentageWidget.razor.cs` |
| D6 | Edit profile and the nav menu call `api/Attachment/*`. No `AttachmentController` exists. | `EditProfilePage.razor.cs:34-36`, `NavMenu.razor.cs:81` |
| D7 | Sign up, forgot password, reset password and email confirmation pages call nothing (the calls are commented out). | `IIdentityController.cs` |
| D8 | The token refresh code is commented out. The server has no refresh endpoint. | `AuthenticationManager.cs:63` |
| D9 | `IMinimalApiController` is dead template code. | `Controllers/IMinimalApiController.cs` |
| D10 | The nav menu links to `/products` and `/categories`. The pages do not exist. | `NavMenu.razor.cs` |
| D11 | A failed `ServiceResponse` (400, no `Request-ID`) shows a generic error, not its Persian message. | `ExceptionDelegatingHandler.cs` |
| D12 | Every proxy call in Interactive Server mode fails: "BaseAddress must be set". `AddHttpClient()` registers a plain `HttpClient` first, so the server's `TryAddTransient` with the base address never runs. Found in the live run. | `Program.Services.cs:97,205` |
| D13 | Blazor sign-in sends `LoginProvider = 0`. The login record is not saved (null key). Found in the live run. | `AuthenticationService` log |
| D14 | The panel is for admins, but no page or endpoint checks a role. The `AdminsOnly` policy is unused and no `Admin` role exists. With OTP sign-in, any phone number opens the panel. Found after the user said the panel is for admins. | `IServiceCollectionExtensions.cs:15`, `DashboardPage.razor.cs:3` |

## Decisions (the user, 2026-10-28)

* Delete the sign up, forgot password, reset password and email confirmation pages.
* The Blazor client is the **admin panel** (ADR-0005). This replaces two earlier decisions of
  this cycle: OTP sign-in and per-user order stats.
  * Admins sign in with a password through `GetToken`. The panel has no OTP sign-in.
  * Admin pages and endpoints use the `AdminsOnly` policy.
  * The dashboard shows business-wide order counts.
  * A migration seeds the `Admin` role. An operator gives the role with the SQL in ADR-0005.

## Requirements

* FR-1: Sign-in uses `GetToken` (phone number and password). The client stores the token only
  when it has `role: Admin`, else it shows `ForbiddenException`.
* FR-2: `GET api/Dashboard/GetOverallAnalyticsStatsData` returns `TotalOrders`,
  `Last30DaysOrders` and `PaidOrders` for all orders. Only `AdminsOnly` callers get them (else 403).
* FR-3: The client calls no route that the server does not map.
* FR-4: A failed `ServiceResponse` shows its `message` in the client.
* FR-5: New client text is a resource key with a Persian value (ADR-0003).
* FR-6: The `SeedAdminRole` migration adds the `Admin` role when it is missing.

## Out of scope

* Order, payment and people UI in Blazor (the client interfaces stay empty).
* Unused resource keys and DTOs of the deleted pages (`SignUpRequestDto`, ...).
* An unknown `GET /api/*` returns 302 to the HTML not-found page, not 404.
