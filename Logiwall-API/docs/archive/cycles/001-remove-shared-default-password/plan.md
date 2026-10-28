# Cycle 001 — plan

## Contents

- L27 Git workflow
- L34 STOP conditions
- L48 Commands you will need
- L57 Tasks
  - L59 Task 1: Create OTP users without a password and check the result
  - L101 Task 2: Add a data migration that clears the shared password from unregistered users
- L126 Test plan
- L138 Done criteria

> **Executor instructions**: Follow this plan step by step. Run every
> verification command and confirm the expected result before moving to the
> next step. If anything in the "STOP conditions" section occurs, stop and
> report — do not improvise. When done, update the status row for this plan
> in this cycle's `tasks.md` — unless a reviewer dispatched you and told you they
> maintain the index.
>
> **Drift check (run first, from the git root `D:\Own Projects\Logiwall`)**:
> `git diff --stat 594e1c5..HEAD -- Logiwall-API/src/Logiwall.Infrastructure/Services/Identity Logiwall-API/src/Logiwall.Infrastructure/Data/Migrations`
> If any in-scope file changed since this plan was written, compare the
> "Current state" excerpts against the live code before proceeding; on a
> mismatch, treat it as a STOP condition.

## Git workflow

- Branch: `advisor/001-remove-shared-default-password`
- History has a single "First Commit", so there is no convention to match. Use
  short imperative messages, e.g. `Create OTP users without a password`.
- Do NOT push or open a PR.

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

## Commands you will need

| Purpose | Command (from `Logiwall-API/`) | Expected on success |
|---|---|---|
| Build | `dotnet build src/Logiwall.Server/Logiwall.Server.csproj` | exit 0, "Build succeeded" (≈40 pre-existing nullable warnings are normal) |
| Restore EF tool | `cd src/Logiwall.Server && dotnet tool restore` | "Restore was successful" |
| Add migration | `cd src/Logiwall.Server && dotnet ef migrations add ClearSharedDefaultPasswords --project ../Logiwall.Infrastructure --startup-project . --output-dir Data/Migrations` | "Done." |
| Script migration (no DB needed) | `cd src/Logiwall.Server && dotnet ef migrations script 20240928085439_AlterOrderPostalCodes ClearSharedDefaultPasswords --project ../Logiwall.Infrastructure --startup-project .` | prints SQL containing your `UPDATE` |

## Tasks

### Task 1: Create OTP users without a password and check the result

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

### Task 2: Add a data migration that clears the shared password from unregistered users

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
- [ ] this cycle's `tasks.md` status row updated
