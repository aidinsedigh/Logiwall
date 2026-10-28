> Detail for `instruction.md` §11. Load when you write async or I/O code, pass a
> `CancellationToken`, or register a service in DI.

# 11. Async, Cancellation and Dependency Injection

## 11.1 Async and cancellation

All I/O-bound operations must be asynchronous.

Pass the `CancellationToken` through the whole request path:

```text
Controller action (CancellationToken cancellationToken)
    ↓
I<X>Service method
    ↓
AppDbContext / HttpClient / provider
```

* The controller actions already take a `CancellationToken`. Pass it on. Many services do not
  take one yet; add the parameter when you change a service method (plans 013 and 014).
* Do not use `.Result` or `.Wait()` in request processing.
* Do not use `Task.Run()` to make server-side I/O look asynchronous.
* Give every outgoing HTTP call a timeout (plan 013).

## 11.2 Dependency injection

Use Microsoft's built-in dependency injection.

* Register an Infrastructure service in
  `src/Logiwall.Infrastructure/Services/DependencyInjection.cs` (`AddInfrastructure`). Follow the
  lifetime of the services around it.
* Services use constructor injection through a primary constructor (§14.3).
* Controllers, `AppControllerBase` and `ServerExceptionHandler` use `[AutoInject]` fields
  (Bit.SourceGenerators). Keep that pattern in those classes.
* Avoid the service locator pattern. Do not resolve services from `IServiceProvider` in new code.
  Plan 011 removes the existing use in `OrderService`.
* Avoid hidden dependencies and global mutable state.
* Do not create a service interface without a reason. A service that a controller calls has its
  interface in `Logiwall.Application/Abstractions/<Area>/`.
