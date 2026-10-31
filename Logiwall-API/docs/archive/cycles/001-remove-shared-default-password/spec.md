# Cycle 001 — OTP-created accounts have no password, and existing default passwords are cleared: spec

## Contents

- L19 Summary
- L28 Why this matters
- L42 Repo facts you need
- L58 Current state
- L110 Scope
- L124 Maintenance notes

Status: DONE (see `tasks.md`). Read `tasks.md` first, then `plan.md`. Read
this file by section when a task points at it.

This cycle was plan 001 of the audit of 2026-10-27 (commit `594e1c5`). The notes that
apply to more than one audit cycle (order, operator actions, product decisions) are in
`docs/archive/audit-2026-10-27.md`.

## Summary

- **Priority**: P1 (critical security)
- **Effort**: S
- **Risk**: LOW
- **Depends on**: none
- **Category**: security
- **Planned at**: commit `594e1c5`, 2026-10-27

## Why this matters

Users sign in with a phone number and a one-time SMS code (OTP). When
`SendOtpCode` sees a phone number for the first time, it creates an Identity user
with a **hardcoded password that is the same for every account**. The anonymous
endpoint `POST api/Identity/GetToken` accepts username (phone number) plus
password. So anyone who knows a victim's phone number can get a bearer token for
any user who never finished `Person/Register`, which is the only flow that changes
the password. An attacker can also pre-claim an account: request an OTP for the
victim's number, sign in with the shared password, then call `Register` to set
their own password. After this plan, OTP-created users have **no password at
all**, so password sign-in fails for them, and existing users still on the shared
password have it cleared.

## Repo facts you need

- The git root is `D:\Own Projects\Logiwall`. It contains `Logiwall-API/` (this
  repo's backend) and `Logiwall-Front/` (a separate React app, which you must not
  touch). All paths below are relative to `Logiwall-API/`, and all commands run
  from `Logiwall-API/` unless stated otherwise.
- Stack: .NET 8, ASP.NET Core, EF Core 8 (SQL Server), ASP.NET Core Identity. The
  solution was generated from the "bit-bp" template.
- Do **not** build `Logiwall.sln`: it contains a MAUI project that needs mobile
  workloads. Build only the server project; it pulls in every backend project.
- `ServiceResponse<T>` (`src/Logiwall.Shared/Dtos/Global/ServiceResponse.cs`) has
  a misspelled `Succuss` property. It is a wire contract, so **do not rename it**.
- At planning time the working tree had uncommitted local edits to `global.json`,
  `Logiwall.sln`, `Logiwall.Web.slnf` and `src/Logiwall.Server/appsettings.json`.
  They are not part of this plan, so do not commit them.

## Current state

- `src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` — OTP
  send and confirm logic.
  - Line 17 declares a private field `defaultPassword` holding a short numeric
    literal. **Never copy that literal into code, comments, commit messages,
    tests or reports.** Refer to it only as "the shared default password".
  - Lines 46–58 (inside `SendOtpCodeAsync`):
    ```csharp
    var user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);

    if (user == null || user.IsArchive)
    {
        var result = await userManager.CreateAsync(new User
        {
            UserName = requestDto.PhoneNumber,
            NormalizedUserName = requestDto.PhoneNumber,
            PhoneNumber = requestDto.PhoneNumber,
            Active = true
        }, defaultPassword!);
        user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);
    }
    ```
    `result` is never checked.
  - The method's failure shape, used at lines 82–91, is the pattern to reuse:
    ```csharp
    return new ServiceResponse<bool>
    {
        Data = false,
        Succuss = false,
        Message = localizer.GetString(nameof(AppStrings.OtpServiceIsNotConnected))
    };
    ```
- `src/Logiwall.Server/Controllers/Identity/IdentityController.cs:187-213` —
  `GetToken` calls `signInManager.PasswordSignInAsync(UserName, Password, ...)`.
  The controller is `[AllowAnonymous]`. **Leave this file alone.** Once a user has
  no password hash, `PasswordSignInAsync` fails for them on its own.
- `src/Logiwall.Server/Controllers/People/PersonController.cs:42-43` — `Register`
  sets the user's real password with `GeneratePasswordResetTokenAsync` and then
  `ResetPasswordAsync`. That works whether or not the user had a password before.
  **Leave this file alone.**
- Every user who completed `Register` has a row in the `People` table
  (`Person.UserId`). A user **without** a `People` row is an OTP-created user who
  still has the shared default password.
- Migrations live in `src/Logiwall.Infrastructure/Data/Migrations`, namespace
  `Logiwall.Infrastructure.Data.Migrations`. Data fixes are written as raw SQL in
  `migrationBuilder.Sql(...)`; see `20240924213908_AddAccountInfoInAppSettings.cs`
  for an example. The Identity users table is `Users` (renamed in `AppDbContext.cs:76`).
- The `dotnet-ef` local tool manifest is at
  `src/Logiwall.Server/.config/dotnet-tools.json` (version 8.0.2), so EF commands
  must run from `src/Logiwall.Server`.

## Scope

**In scope** (the only files you should modify or create):
- `src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs`
- `src/Logiwall.Infrastructure/Data/Migrations/<timestamp>_ClearSharedDefaultPasswords.cs` (create)
- `src/Logiwall.Infrastructure/Data/Migrations/<timestamp>_ClearSharedDefaultPasswords.Designer.cs` (generated)
- `src/Logiwall.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` (only if the tool rewrites it; it should not change)

**Out of scope** (do NOT touch):
- `IdentityController.cs` and `PersonController.cs`. Their behavior becomes correct once no shared password exists.
- The archived-user branch semantics (`user.IsArchive`). Plan 008 redefines them, so keep today's behavior for archived users.
- Rate limiting and OTP attempt limits (plan 007).
- The password policy in `appsettings.json`. Changing it would break the frontend's Register form.

## Maintenance notes

- **Operator action (outside code):** deploy this migration promptly. Until it
  runs, every unregistered account in production is still exposed. If logs show
  `GetToken` successes for users without a `People` row, treat those accounts as
  possibly compromised.
- Edge case this migration does not cover: a user who has a `People` row but whose
  `ResetPasswordAsync` failed during Register (`PersonController.cs:42-50`) still
  has the shared password. An operator can find those users with a one-off script
  using `PasswordHasher<User>.VerifyHashedPassword`. That is deliberately not
  automated here, because automating it would put the literal back into the code.
- Reviewers: confirm the literal is gone from source, and that no test fixture
  reintroduces it.
- Direction note: the frontend calls `api/Person/ChangePassword`, which doesn't
  exist. Whether password login should exist at all (OTP-only vs real passwords)
  is an open product decision. This plan makes both options safe.
