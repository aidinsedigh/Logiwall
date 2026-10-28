> Detail for `instruction.md` §14. Load before you commit, or as a general code-quality checklist.

# 14. Git and Code Quality

## 14.1 Git and commit conventions

The git root is `D:\Own Projects\Logiwall`. It holds `Logiwall-API/` and `Logiwall-Front/`.

Git commits must follow **Conventional Commits**.

Format:

```text
<type>(<scope>): <description>
```

Examples:

```text
feat(order): add order cancellation
fix(payment): verify payment on the server
refactor(identity): simplify otp sign-in
test(order): add ownership tests for order list
docs(api): document setup and commands
perf(order): page the order list query
build(deps): patch dotnet 8 packages
ci(github): add build and test workflow
chore(config): remove committed secrets
```

Allowed commit types:

```text
feat
fix
refactor
test
docs
perf
build
ci
chore
```

Use the scope when it gives useful context: an area (`order`, `payment`, `identity`, `people`),
a provider (`tipax`, `sep`, `sms`) or a tool (`deps`, `github`, `config`).

The commit description must:

* be concise
* use imperative style
* describe the actual change
* not end with a period

Do not create meaningless commits such as `update`, `changes`, `fix stuff`, `work` or `temp`.

If a change contains unrelated concerns, split it into logical commits.

Do not commit unless the user asks. Do not push unless the user asks. When a plan names a
branch, use that branch.

Before you commit, inspect:

```text
git status
git diff
```

Never commit:

* secrets or credentials (check `src/Logiwall.Server/appsettings*.json` for a connection string with a password)
* local configuration that contains secrets
* log files (`src/Logiwall.Server/Logs/`)
* generated files that git must not track (`bin/`, `obj/`)
* unrelated changes

---

## 14.2 Code quality

Prefer:

* explicit code
* meaningful names
* small focused classes
* small focused methods
* immutable data where appropriate
* guard clauses
* nullable reference types
* analyzers (Bit.CodeAnalyzers is on)
* readable code

Avoid:

* clever code
* premature abstraction
* premature optimization
* god classes
* god services
* generic repositories
* unnecessary base classes
* unnecessary patterns
* unnecessary comments
* commented-out code (plan 015 removes the existing blocks; do not add new ones)

Comments explain **why**, not obvious implementation details.

Never rename `Succuss`: it is a wire contract with `Logiwall-Front`. Do not rename other
misspelled names (`OrderInqueryOption`, `GetOptionnsRequestDto`, ...) inside an unrelated task.
A rename of an entity also renames a table, so it needs its own plan and a migration.

---

## 14.3 Primary constructors and records

### Primary constructors

Always use a primary constructor. Use a normal constructor only when a primary constructor
cannot do the job:

* The type has more than one constructor that do not chain to one primary constructor (the
  exception types in `src/Logiwall.Shared/Exceptions/`).
* The constructor runs statements, not only assignments (`LogiwallApiFactory` sets an
  environment variable).
* The constructor is generated (`*.Designer.cs`), or the type is a Client template file.
* The class uses `[AutoInject]` fields (controllers, `AppControllerBase`,
  `ServerExceptionHandler`, ...). Keep that pattern (§11.2).

Use the parameters directly in the methods. Do not copy a parameter into a field with the same
value. Add a field only when you change the value once, for example `IOptions<T>.Value`:

```csharp
public class SMSService(IHttpClientFactory httpClientFactory, IOptions<SmsOptions> smsOptions, ILogger<SMSService> logger) : ISMSService
{
    private readonly SmsOptions smsOptions = smsOptions.Value;
}
```

Do not assign to a primary constructor parameter. The compiler does not make it `readonly`.

### Records

Use `record` for DTOs, models and contracts: the types in `src/Logiwall.Shared/Dtos/`, email
models (`ResetPasswordModel`) and test request/response types.

* Keep the `{ get; set; }` or `{ get; init; }` properties. Model binding, `System.Text.Json`,
  Mapperly and the validation attributes use them.
* Use a positional record (`record PagedResult<T>(T[] Items, long TotalCount)`) only for a small
  type without validation attributes or `[Display]` names.
* Do not log a whole record. Its generated `ToString()` writes every property, also passwords
  and tokens (§10).

Keep these types as `class`:

* EF Core entities in `src/Logiwall.Domain/Entities/`. Record value equality breaks change
  tracking and navigation collections.
* Options and settings classes (`AppSettings`, `SmsOptions`, ...). They hold secrets, and a
  record's `ToString()` writes them.
* Services, providers, controllers, middleware, `JsonSerializerContext` and exceptions.
