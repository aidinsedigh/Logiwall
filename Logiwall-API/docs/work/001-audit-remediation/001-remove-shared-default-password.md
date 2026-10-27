# Plan 001: OTP-created accounts have no password, and existing default passwords are cleared

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in `docs/work/001-audit-remediation/tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/Identity Logiwall-API/src/Logiwall.Infrastructure/Data/Migrations`
> If any in-scope file changed since this plan was written, compare the
> "Current state" excerpts against the live code before proceeding; on a
> mismatch, treat it as a STOP condition.

## Status

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

## Commands you will need

| Purpose | Command (from `Logiwall-API/`) | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0, "Build succeeded" (≈40 pre-existing nullable warnings are normal) |
| Restore EF tool | `cd src/Logiwall.Server && dotnet tool restore` | "Restore was successful" |
| Add migration | `cd src/Logiwall.Server && dotnet ef migrations add ClearSharedDefaultPasswords --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` | "Done." |
| Script migration (no DB needed) | `cd src/Logiwall.Server && dotnet ef migrations script 20240928085439_AlterOrderPostalCodes ClearSharedDefaultPasswords --project ../Logiwall.Infrastructure --startup-project .` | prints SQL containing your `UPDATE` |

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

## Git workflow

- Branch: `advisor/001-remove-shared-default-password`
- History has a single "First Commit", so there is no convention to match. Use
  short imperative messages, e.g. `Create OTP users without a password`.
- Do NOT push or open a PR.

## Steps

### Step 1: Create OTP users without a password and check the result

In `AuthenticationService.cs`:

1. Delete the `defaultPassword` field (line 17).
2. Replace the block shown in "Current state" with:
   ```csharp
   var user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);

   if (user == null || user.IsArchive)
   {
       // OTP users have no password; password sign-in (GetToken) is only possible after Person/Register sets one.
       var createResult = await userManager.CreateAsync(new User
       {
           UserName = requestDto.PhoneNumber,
           NormalizedUserName = requestDto.PhoneNumber,
           PhoneNumber = requestDto.PhoneNumber,
           Active = true
       });

       if (createResult.Succeeded is false && user is null)
       {
           return new ServiceResponse<bool>
           {
               Data = false,
               Succuss = false,
               Message = localizer.GetString(nameof(AppStrings.OtpServiceIsNotConnected))
           };
       }

       user = await userManager.FindByNameAsync(requestDto.PhoneNumber!);
   }
   ```
   The `&& user is null` keeps today's behavior for archived users: creation fails
   for them because the username already exists, and the flow then continues with
   the archived user. Plan 008 changes that, so don't change it here.

**Verify**:
- `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` → exit 0.
- `grep -n "defaultPassword" src/Logiwall.Infrastructure/Services/Identity/AuthenticationService.cs` → no output.
- `grep -rn "CreateAsync(new User" src/Logiwall.Infrastructure` → exactly one match, and it has no second argument.

### Step 2: Add a data migration that clears the shared password from unregistered users

1. Restore the EF tool and add an empty migration (see the commands table).
   The generated `Up`/`Down` should be empty because the model did not change.
2. Replace `Up` with:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       // Users created by the OTP flow before plan 001 share one hardcoded password.
       // Users without a People row never ran Person/Register, so they still have it. Clear it.
       migrationBuilder.Sql(@"
           UPDATE [dbo].[Users]
           SET [PasswordHash] = NULL,
               [SecurityStamp] = CONVERT(nvarchar(36), NEWID())
           WHERE [PasswordHash] IS NOT NULL
             AND [Id] NOT IN (SELECT [UserId] FROM [dbo].[People]);");
   }
   ```
3. Leave `Down` empty, with the comment `// Irreversible by design: the old shared password must not be restored.`

**Verify**:
- The script command in the table → its output contains `UPDATE [dbo].[Users]` and `[PasswordHash] = NULL`.
- `git diff --stat -- src/Logiwall.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` → no changes. If it changed, see the STOP conditions.
- `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` → exit 0.

## Test plan

The repo has no test project yet; plan 005 creates one. Plan 005 already lists
the regression tests for this plan: "an OTP-created user cannot obtain a token
via `GetToken`", and "a user who completed Register can". So **write no tests
here**. Do the manual check below only if a local SQL Server is configured;
otherwise skip it and say so in your report:

1. `dotnet ef database update` (same flags as the migration commands), then run the server (`dotnet run --project src/Logiwall.Server`).
2. `POST http://localhost:5030/api/Identity/SendOtpCode` with body `{"phoneNumber":"09000000001","channel":"pwa","loginProvider":20}` → 200.
3. In SQL, `SELECT PasswordHash FROM Users WHERE UserName='09000000001'` → `NULL`.

## Done criteria

- [ ] `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` exits 0
- [ ] `grep -rn "defaultPassword" src/` returns no matches
- [ ] A migration named `*_ClearSharedDefaultPasswords.cs` exists, and its `Up` contains the `UPDATE [dbo].[Users]` statement
- [ ] `AppDbContextModelSnapshot.cs` is unchanged
- [ ] `git status` shows changes only to in-scope files (ignore the pre-existing uncommitted edits listed in "Repo facts")
- [ ] The shared password literal appears nowhere in the diff, the commit messages or your report
- [ ] `docs/work/001-audit-remediation/tasks.md` status row updated

## STOP conditions

Stop and report back (do not improvise) if:

- `AuthenticationService.cs` doesn't match the excerpt above.
- `dotnet ef migrations add` changes `AppDbContextModelSnapshot.cs` or generates
  non-empty `Up`/`Down` code. That means the model has drifted from the
  snapshot, and the migration would carry unrelated schema changes.
- `dotnet ef` fails because it can't start the host (for example a certificate
  or config error). Report the exact error. Do not edit `Program*.cs` to work
  around it.
- The table is not `[dbo].[Users]`, or `People` has no `UserId` column (check
  `AppDbContextModelSnapshot.cs`).

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
