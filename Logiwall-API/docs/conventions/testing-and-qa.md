> Detail for `instruction.md` §12. Load when you write unit or integration tests, or an
> endpoint's QA file.

# 12. Testing and QA Files

## 12.1 Testing

There are no test projects yet. Plan 005 in `docs/work/001-audit-remediation/` creates them.
Until it lands, verify a change with `dotnet build src/Logiwall.Server/Logiwall.Server.csproj`
and state in your report that no tests ran.

After plan 005, the layout is:

```text
tests/
├── Logiwall.UnitTests/          services and rules with no database
└── Logiwall.IntegrationTests/   real SQL Server through Testcontainers (needs Docker)
```

### Unit tests

Use xUnit. Use unit tests for business rules, mappers, validation and isolated service logic.
Unit tests must be fast, deterministic and isolated.

### Integration tests

Use xUnit and Testcontainers with a real SQL Server container. Use them for anything that
touches `AppDbContext`, migrations or the HTTP pipeline.

* Do not use EF InMemory or SQLite (`ef-core.md` §6.2).
* Do not call a test an integration test if it mocks the database.
* Mock external providers (SEP, MyTipax, SMS) at the `I*Provider` / `ISMSService` interface.
  Never call a real provider from a test.

### Regression tests

Each security fix gets a test that fails before the fix. Example: a test that a user cannot read
another user's order (plan 002).

## 12.2 QA files

Each new or changed endpoint must have a QA file.

* Name it `<Action>.QA.md` and put it beside the controller, for example
  `src/Logiwall.Server/Controllers/Order/CreateOrder.QA.md`.
* Write it as an expert QA engineer. Apply `explaining-output.md` (Simplified Technical English).

It must contain:

* acceptance criteria
* positive scenarios
* negative scenarios
* edge cases
* authorization and resource ownership scenarios
* validation scenarios
* concurrency and idempotency scenarios where relevant
* failure scenarios (provider down, timeout, database error)
* integration considerations (front-end contract, external providers)

A change is not complete until its acceptance criteria are covered. Do not write QA files for
endpoints you did not change.
