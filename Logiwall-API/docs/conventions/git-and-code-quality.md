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
