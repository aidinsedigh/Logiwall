> Detail for `instruction.md` §6. Load when you touch EF Core code, migrations, entity
> configuration, or the database schema.

# 6. Entity Framework Core and SQL Server

Logiwall uses **EF Core 8** with **Microsoft SQL Server** only.

## 6.1 One `AppDbContext`

* There is one `DbContext`: `AppDbContext` in `src/Logiwall.Infrastructure/Data/AppDbContext.cs`.
  Its namespace is `Logiwall.Infrastructure.Data`.
* Put entity configuration in `src/Logiwall.Infrastructure/Data/Configurations/<Area>/<Entity>Configuration.cs`
  as an `IEntityTypeConfiguration<T>`. Do not configure entities with attributes when a
  configuration class exists for that entity.
* Migrations live in `src/Logiwall.Infrastructure/Data/Migrations/`. Use the commands in
  `instruction.md` §1.1. Nothing applies migrations automatically at start-up.
* `AppDbContext.SaveChangesAsync` converts `DbUpdateConcurrencyException` to `ConflictException`.
  Do not catch the concurrency exception again in a service.

## 6.2 SQL Server only

The model uses SQL Server features: the sequence `dbo.OrderSequence`, `HasDefaultValueSql` with
`FORMAT`, and filtered unique indexes. As a result:

* Do not use EF InMemory or SQLite for tests. Use real SQL Server through Testcontainers (§12).
* Do not remove a SQL Server feature only to make a test provider work.

## 6.3 Query and write rules

* Use the asynchronous APIs (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`).
* Pass the `CancellationToken` to each EF call (§11).
* Use `AsNoTracking()` for read-only queries.
* Use projections (`Select`) for read queries. Do not load whole graphs to return three fields.
* Avoid N+1 queries. Load related data in one query.
* Bound every list query: page it or limit it (`instruction.md` §7, plan 014).
* Filter every query by ID with the owner of the row (§9). Example:
  `t.Customer.Person.UserId == userId`.
* Put more than one `SaveChangesAsync` that must succeed together in one transaction.
* Inspect the generated SQL for a complicated query.

## 6.4 Schema changes

* Configure relationships, keys and indexes explicitly.
* Use database constraints when they can safely enforce an important invariant.
* Consider nullability, data types, indexes and concurrency for each new column.
* Never put credentials or environment values in a migration's seed data (plan 003).
* Review the generated migration before you commit it. Check that it changes only what you meant.
