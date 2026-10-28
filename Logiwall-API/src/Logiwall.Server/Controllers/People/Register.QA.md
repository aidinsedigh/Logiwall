# QA: `POST api/Person/Register`

The endpoint creates the person and customer rows for an OTP user, and sets the password.
Cycle 019 (E2E B6) also writes the e-mail to the Identity user, because `GetUserProfile` reads
`User.Email`.

Request: `{ name, lastName, nationalCode, email?, password, confirmPassword }`.

## Acceptance criteria

1. With an `accessToken` from `ConfirmationOtpCode`, the call returns 200.
2. If `email` is sent, `GetUserProfile` returns it.
3. The person row, the e-mail and the password save in one transaction.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Register with an e-mail. Read the profile. | 200. `email` is the sent value. |
| P2 | Register without an e-mail. | 200. `email` is null. |
| P3 | Sign in with `GetToken` and the new password. | 200 |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | Register twice. | 400 `YouRegisteredBefore` |
| N2 | Use a national code that another person has. | 400 `NationalCodeIsTaken` |
| N3 | Send an invalid e-mail. | 400, e-mail validation message. No person row is saved. |
| N4 | Send different `password` and `confirmPassword`. | 400 validation error |
| N5 | Call with no token. | 401 |

## Edge cases

* An e-mail that another user has: Identity rejects it if `RequireUniqueEmail` is on, and the transaction rolls back.

## Authorization and resource ownership

The user comes from the token. The phone number comes from `User.UserName`, not from the body.

## Validation

`RegisterPersonRequestDto`: required names and national code, password `MinLength(6)`, `Compare`.

## Concurrency and idempotency

A second call returns `YouRegisteredBefore`. The unique index on `NationalCode` is the last guard.

## Failure scenarios

Password policy failure: 400 `AnErrorOccurrdWhileRegisteration`, and the transaction rolls back.

## Integration considerations

The front calls this with the token from `ConfirmationOtpCode` (`setToken` in `otp.ts`).

Automated tests: `ProfileTests.Register_email_shows_in_the_profile`, `RegisterTests`.
