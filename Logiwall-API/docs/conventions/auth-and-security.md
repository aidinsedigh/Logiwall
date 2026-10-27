> Detail for `instruction.md` §9. Load when you touch authentication, authorization, a query by
> ID, secrets, logging of HTTP traffic, or an anonymous endpoint.

# 9. Authentication, Authorization and Security

## 9.1 Authentication and authorization

* Logiwall uses ASP.NET Core Identity with bearer tokens (`src/Logiwall.Server/Program.Services.cs`).
* Every controller requires an authenticated user:
  `app.MapControllers().RequireAuthorization()` in `src/Logiwall.Server/Program.Middlewares.cs`.
* Only `IdentityController` and `CommonController` are `[AllowAnonymous]`. Do not add
  `[AllowAnonymous]` to another controller or action without the user's decision.
* Take the user from the token with `User.GetUserId()`. Never trust a user ID, customer ID or
  person ID from the request.

## 9.2 Resource ownership (IDOR)

Every query that loads a row by ID must also check that the caller owns the row. Put the owner
predicate in the same query, for example:

```csharp
.Where(t => t.Id == requestDto.OrderId && t.Customer.Person.UserId == userId)
```

If the row does not exist **or** the caller does not own it, return the same "not found" result.
Do not tell the caller that the row exists. Plan 002 applies this rule to the existing code.

## 9.3 Secrets

* Never put credentials in code, migrations, `appsettings*.json` or log output.
* Read secrets from configuration: user-secrets in development, Key Vault in Azure
  (`src/Logiwall.Iac/`). Plan 003 moves the existing credentials there.
* Never reintroduce a shared default password for accounts (plan 001).
* `src/Logiwall.Server/IdentityCertificate.pfx` is the template's public test certificate. It is
  accepted only in Development. Production supplies its own certificate.
* Do not commit a local connection string with a password. Check `git diff` for
  `appsettings.json` before each commit.

## 9.4 Logging and data exposure

* Never log header values, tokens, OTP codes, passwords, API keys or request/response bodies.
  Log the URI, the status code and the IDs you need (§10).
* Do not return entities or internal fields to the client (§7).
* Do not return `ex.Message` to the client (§8).

## 9.5 Checklist

Always consider:

* authentication
* authorization and resource ownership
* input validation
* SQL injection (no raw SQL with concatenated input; no dynamic field names from the client)
* rate limiting for OTP and login (plan 007)
* sensitive logging
* secret management
* token security
* CORS and forwarded headers (plan 009)
* HTTPS and secure headers
* server-side verification of payments (plan 012)

Do not assume framework defaults solve every application-specific security requirement.
