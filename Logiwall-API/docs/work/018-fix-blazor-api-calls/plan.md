# Cycle 018 — plan

Branch: `advisor/cycles-001-017` (the user's current branch). Commit when the user asks.

## Drift check (run first)

* `IIdentityController` declares `SignIn`, and the server has `SendOtpCode` and
  `ConfirmationOtpCode`.
* `DashboardController` implements only `GetOverallAnalyticsStatsData`.

STOP if either is not true. Then the review is out of date.

## Task 1: sign-in (D1, D2, D7, D8, D11)

Task 4 replaced the first OTP version of this task.

* `IIdentityController`: replace `SignIn` and the commented methods with `GetToken` (the server
  action name). Keep the `=> default!` body, like the other proxies.
* `AuthenticationManager`: `SignIn(SignInRequestDto)`. Store only the access token. Delete the
  commented refresh block.
* `SignInPage`: phone number and password. Remove the sign-up and forgot-password links.
* `Header.razor`: remove the sign-up link.
* Delete `SignUpPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `EmailConfirmationPage`.
* `ExceptionDelegatingHandler`: a failed JSON response with no `Request-ID` and a `message`
  throws `BadRequestException(message)`.

## Task 2: order stats (D3, D4, D5, D10)

* `OverallAnalyticsStatsDataResponseDto`: `TotalOrders`, `Last30DaysOrders`, `PaidOrders`.
* `IOrderService.GetOrderStatsAsync(CancellationToken)`: counts all orders (Task 4).
  A paid order has an `OrderPayment` with `PaymentStatus.Success`.
* Delete the three product widgets, their interface methods and DTOs.
* `NavMenu`: Home, Dashboard, Edit profile, Terms.
* Integration tests (Task 4). QA file
  `Controllers/Dashboard/GetOverallAnalyticsStatsData.QA.md`.
* New resource keys: `TotalOrders`, `Last30DaysOrders`, `PaidOrders`.

## Task 3: dead attachment and template calls (D6, D9)

* `EditProfilePage`: remove the profile image upload, image and remove calls.
* `NavMenu`: remove the profile image URL.
* Delete `IMinimalApiController` and its paragraph in `Controllers/Readme.md`.

## Found in the live run (D12, D13)

* `Program.Services.cs` `AddBlazor`: `AddTransient` (not `TryAddTransient`) for the base-address `HttpClient`.
* `SignInPage`: send `LoginProvider.Logiwall`, like the React app.

## Task 4: admin panel (D14, ADR-0005)

* `IIdentityController.GetToken`; `SignInPage` with phone number and password; send
  `LoginProvider.Logiwall`.
* `AuthenticationManager.SignIn`: reject a token without `role: Admin` (`ForbiddenException`).
  `ParseTokenClaims` reads a JSON array of roles as one claim for each role.
* `SignInRequestDto.UserName`: display name `PhoneNumber`, not `Email`.
* `[Authorize(Policy = "AdminsOnly")]` on `DashboardController`, `DashboardPage`, `EditProfilePage`.
* `GetOrderStatsAsync` counts all orders.
* Migration `SeedAdminRole`: insert `Admin` into `dbo.Roles` when it is missing (idempotent SQL).
* Revert the OTP-only changes: OTP DTO attributes, OTP `AppJsonContext` entries, keys `OtpCode`,
  `SendOtpCode`, `PhoneNumberIsNotValid`.
* Tests (`DashboardStatsTests`): admin sees the change in all orders; the admin token has
  `role: Admin`; a customer gets 403; no token gets 401.
* ADR-0005 with the operator SQL. Update `instruction.md` §1/§2, `auth-and-security.md` §9.1.

## Verification

* `dotnet build src/Logiwall.Server/Logiwall.Server.csproj`: 0 errors.
* `dotnet test tests/Logiwall.UnitTests`, and the new integration test when Docker runs.
* Run the server. Sign in as a customer (rejected) and as an admin. Open `/dashboard` and `/edit-profile`. No 404/405/302 in the
  network log.
