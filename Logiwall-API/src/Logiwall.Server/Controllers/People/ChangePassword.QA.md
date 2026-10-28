# QA: `POST api/Person/ChangePassword`

New in cycle 019 (E2E C5). The signed-in user changes their password. The body is the one the
React page sends: `{ oldPassword, newPassword }` (`Logiwall-Front/src/features/profile/api/change-password.ts`).

Response: `ServiceResponse<bool>`.

## Acceptance criteria

1. The right old password and a valid new password return 200 with `PasswordChangedSuccessfullyMessage`.
2. Then `GetToken` accepts the new password and rejects the old one.
3. A wrong old password returns 400 with a Persian message, and the password hash does not change.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Register with `Strong-Pass-1`. Change it to `Other-Pass-2`. | 200 "تغییر رمز عبور با موفقیت انجام شد." |
| P2 | Call `GetToken` with `Other-Pass-2`. | 200 |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | Send a wrong `oldPassword`. | 400. `AppIdentityErrorDescriber` gives the Persian `PasswordMismatch` text. |
| N2 | Send a `newPassword` shorter than 6 characters. | 400 validation error |
| N3 | Send a `newPassword` that fails the Identity policy. | 400 with the Persian policy messages |
| N4 | Call with no token. | 401 |
| N5 | Call as an OTP user who has not registered (no password). | 400. Identity cannot change a password that does not exist. |

## Edge cases

* The same value for old and new password: Identity accepts it.

## Authorization and resource ownership

The user ID comes from the token only. The body has no user field.

## Validation

`ChangePasswordRequestDto`: both fields `[Required]`, `newPassword` `MinLength(6)`.

## Concurrency and idempotency

A repeat of the same call fails, because the old password is no longer valid.

## Failure scenarios

* Rate limit `auth-verify` (per IP) limits password guesses with a stolen token. Over the limit: 429.
* Database down: 500 with the standard error body.

## Integration considerations

The front shows its own toast and does not read the response. The old bearer tokens stay valid until they expire.

Automated tests: `ProfileTests.ChangePassword_*`.
