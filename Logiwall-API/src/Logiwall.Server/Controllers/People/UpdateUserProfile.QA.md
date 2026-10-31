# QA: `POST api/Person/UpdateUserProfile`

The endpoint updates the signed-in user's profile. Cycle 019 (E2E B1) made it accept the body
the React profile form sends: `{ firstName, lastName, nationalCode, email, gender }`
(`Logiwall-Front/src/features/profile/api/change-user-info.ts`).

## Acceptance criteria

1. The front body returns 200 with `ProfileIsUpdated`.
2. The service never writes `Person.PhoneNumber`. The phone number is the login identity.
3. If the body has no `kind`, the stored `Kind` stays. If the body has no `birthDate`, the stored `BirthDate` stays.
4. `GetUserProfile` then returns the new name, national code, e-mail and gender.

## Positive scenarios

| # | Steps | Expected |
|---|---|---|
| P1 | Register. Send the front body with a new name and e-mail. | 200. `GetUserProfile` shows the new values and the old phone number. |
| P2 | Send the full `UserProfileDto` from `GetUserProfile` with a changed name. | 200 |

## Negative scenarios

| # | Steps | Expected |
|---|---|---|
| N1 | Send a `phoneNumber` that is not the user's. | 200. The phone number does not change. |
| N2 | Send an invalid e-mail. | 400, e-mail validation message |
| N3 | Send no `nationalCode`. | 400 validation error (`[Required]`) |
| N4 | Call before `Register`. | 400 `UserIsNotRegistered` |

## Edge cases

* A `nationalCode` that another person uses breaks the unique index. The answer is 400 `UnknownException`. Not fixed in cycle 019.
* The front sends `gender` 1 (male) or 3 (female). `Constants.Gender` has Male = 3, Female = 4. The numbers round-trip, but the server meaning is wrong. Open item.

## Authorization and resource ownership

* The user ID comes from the token. A body `userId` of another user returns 400 `InvalidUserId` (`PersonController.cs`).

## Validation

`UserProfileDto`: `nationalCode` `[Required, StringLength(11)]`.

## Concurrency and idempotency

The e-mail change and the person rows save in one transaction. The same body twice gives the same result.

## Failure scenarios

Database error: 400 `UnknownException`, and the service logs the exception.

## Integration considerations

The front shows its own success toast and reloads `user-profile-info`.

Automated tests: `ProfileTests.UpdateUserProfile_with_the_front_body_keeps_phone_and_kind`.
