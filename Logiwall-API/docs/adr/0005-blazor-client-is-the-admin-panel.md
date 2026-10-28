# ADR-0005: The Blazor client is the admin panel

**Status:** Accepted, 2026-10-28

## Decision

The Blazor client (`src/Client/*`, hosted by `Logiwall.Server`) is the admin panel. Only users
with the `Admin` role use it. Customers use the React app (`Logiwall-Front`).

* **Sign-in:** admins sign in with a phone number and a password through `api/Identity/GetToken`.
  It has lockout (`lockoutOnFailure: true`) and the `auth-verify` rate limit. The panel has no
  OTP sign-in, because `SendOtpCode` creates a user for any phone number.
* **Client check:** `AuthenticationManager.SignIn` stores the token only when it has the claim
  `role: Admin`. Else it shows `ForbiddenException`. `GetToken` stays open to every user, because
  the React app uses it for the customer password sign-in.
* **Server check:** every admin endpoint has `[Authorize(Policy = "AdminsOnly")]`
  (`RequireRole("Admin")`, `src/Logiwall.Shared/Extensions/IServiceCollectionExtensions.cs`).
  The client check is only for the UI. The server check is the security boundary.
* **Pages:** every panel page that needs a user has `[Authorize(Policy = "AdminsOnly")]`.
* **The role:** the `SeedAdminRole` migration adds the `Admin` row to `dbo.Roles` when it is
  missing. No admin user and no password are in the repository.
* **The seed:** `AdminSeeder` (`src/Logiwall.Infrastructure/Services/Identity/AdminSeeder.cs`)
  runs at start-up. It makes the user in `AdminSeed:PhoneNumber` an admin. It does nothing when
  `AdminSeed:PhoneNumber` or `AdminSeed:Password` is empty.

## Operator step: seed the first admin

Set the two values outside the repository, then start the server:

```
cd src/Logiwall.Server
dotnet user-secrets set "AdminSeed:PhoneNumber" "09xxxxxxxxx"
dotnet user-secrets set "AdminSeed:Password" "<password>"
```

On a server, use the environment variables `AdminSeed__PhoneNumber` and `AdminSeed__Password`.

* A missing user is created with the password and the `Admin` role.
* An existing user gets the `Admin` role. The password is set only when the user has none. The
  seed never replaces a password.
* The log shows `[AdminSeed][Start][{UserId}] Admin user is ready.`, or a warning with the
  Identity error codes (for example a password that fails the password rules).
* The seed does not create a `Person` row. For a new seeded user, `GetUserProfile` returns
  `UserIsNotRegistered` (`PersonService.cs`), so the profile page has no data.

## Operator step: make an existing user an admin with SQL

1. The person signs in once in the React app with OTP.
2. The person sets a password with `Person/Register` (the React registration form).
3. An operator runs this SQL on the database. Change the phone number:

```sql
SET QUOTED_IDENTIFIER ON;
GO
DECLARE @PhoneNumber nvarchar(256) = N'09xxxxxxxxx';

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId])
SELECT u.[Id], r.[Id]
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[NormalizedUserName] = UPPER(@PhoneNumber)
  AND u.[PasswordHash] IS NOT NULL
  AND r.[NormalizedName] = N'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]);

SELECT @@ROWCOUNT AS [RolesGiven];
```

`RolesGiven = 1` means done. `0` means: the phone number is wrong, the user has no password, the
user is already an admin, or the migration did not run. The role is in the token from the next
sign-in. To remove an admin, delete the `UserRoles` row. A token that is already issued keeps
the role until it expires (`BearerTokenExpiration`).

`sqlcmd` starts with `QUOTED_IDENTIFIER OFF`, so the script needs the `SET` line.

## Context

Cycle 018 first gave the panel OTP sign-in and per-user order stats, on the assumption that the
panel was for customers. The user then said the panel is for admins. With OTP, any phone number
could open the panel. Before cycle 018 the sign-in was broken, so the panel was closed only by
accident. No endpoint used the `AdminsOnly` policy, and no `Admin` role existed.

## Consequences

* A new admin endpoint must have `[Authorize(Policy = "AdminsOnly")]` and an integration test
  that a customer token gets 403 (see `DashboardStatsTests.Customer_gets_403`).
* An admin endpoint can return business-wide data, for example the dashboard order counts.
* Admins have a password and can be locked out. A locked admin waits for the lockout to end.
* The panel has no sign-up, forgot-password or OTP page. Password changes happen outside the
  panel.
