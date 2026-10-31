# QA: `POST api/Identity/ConfirmationOtpCode`

The endpoint checks the OTP and returns a bearer token. Cycle 019 changed the response to the
shape the React app reads (`Logiwall-Front/src/features/auth/api/otp.ts:75-88`).

Request: `{ phoneNumber, code, channel, loginProvider }`.

Response `data` (`ConfirmationOtpResponseDto`):

| Field | Meaning |
|---|---|
| `accessToken` | The bearer token. Same protector as `GetToken`. |
| `expiresIn` | The token lifetime in seconds (`BearerTokenExpiration`). |
| `fullName` | The person's name. Null when the user has not registered. |

## Acceptance criteria

1. A correct code returns 200 with `accessToken`, `expiresIn > 0` and the message `OtpLoginSucceeded`.
2. The old field `token` is not in the response.
3. The front sends a new user (no `fullName`) to `/auth/register`, and the token works on `Person/Register`.
4. The front sends a registered user (`fullName` set) to `/app/home`.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Send an OTP to a new number. Confirm with the code. | 200, `fullName` null, `accessToken` set, message "ورود با موفقیت انجام شد." |
| P2 | Call `Person/Register` with `Bearer <accessToken>`. | 200 |
| P3 | Sign in again with OTP. | 200, `fullName` = "name lastName" |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | Confirm with a wrong code. | 400 `OtpCodeIsNotCorrect` |
| N2 | Confirm after 2 minutes. | 400 `OtpCodeIsExpired` |
| N3 | Confirm for a number with no user. | 400 `OtpCodeIsNotCorrect` (no user enumeration) |
| N4 | Confirm for an archived or inactive user. | 400 `AccountIsDisabled` |

## Edge cases

* A code with Persian digits is converted before the compare.
* The sixth wrong guess burns the code (`MaxOtpAttempts`).

## Authorization and resource ownership

`[AllowAnonymous]`. The token belongs to the user of `phoneNumber` only.

## Validation

`ConfirmaionOtpRequestDto` validation. Rate limit `auth-verify` (per IP).

## Concurrency and idempotency

A code works once. The service removes the OTP tokens after a success.

## Failure scenarios

Database down: 500 with the standard error body.

## Integration considerations

The front stores `expiresIn` and signs the user out after that number of seconds (`lib/auth/hooks/index.ts`).

Automated tests: `ProfileTests.ConfirmationOtpCode_returns_the_fields_the_front_reads`, `OtpThrottlingTests`.
