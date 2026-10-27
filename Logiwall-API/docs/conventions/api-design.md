> Detail for `instruction.md` §7. Load when you add or change an endpoint, a DTO, validation or
> a list endpoint.

# 7. API Design, Contracts, Validation and Pagination

## 7.1 Controllers

Logiwall uses **ASP.NET Core MVC controllers**, not Minimal APIs.

* Put a controller in `src/Logiwall.Server/Controllers/<Area>/<X>Controller.cs`.
* Make it `public partial class <X>Controller : AppControllerBase, I<X>Controller`.
  `I<X>Controller` is in `src/Client/Logiwall.Client.Core/Controllers/<Area>/`.
* Use the route `[Route("api/[controller]/[action]")]` and `[ApiController]`.
  `Logiwall-Front` calls these routes. Do not change a route without the front end.
* Get dependencies through `[AutoInject] private I<X>Service <x>Service = default!;`
  (Bit.SourceGenerators). That is why the class is `partial`.
* Get the caller with `User.GetUserId()`. Never take the user ID from the request body.
* Keep the controller thin: call one service method, then return the result.

Example: `src/Logiwall.Server/Controllers/Order/OrderController.cs`.

## 7.2 Contracts

* Put request and response types in `src/Logiwall.Shared/Dtos/<Area>/`. Name them
  `<Action>RequestDto` and `<Action>ResponseDto`.
* Do not return an entity from a controller. Map it to a DTO with a Mapperly mapper in
  `src/Logiwall.Infrastructure/Mapper/`.
* A service returns `ServiceResponse<T>` with `Data`, `Succuss` and `Message`. `Succuss` is
  misspelled, but it is a wire contract with `Logiwall-Front`. **Never rename it.**
* The controller returns `BadRequest(result)` when `Succuss` is `false`, else `Ok(result)`.
* Do not expose fields the client does not need (excessive data exposure).

## 7.3 Validation

* Validate request shape with DataAnnotations on the DTO (`[Required]`, `[MaxLength]`, ...).
  `[ApiController]` returns 400 for an invalid model.
* Validate business rules in the service. Return `Succuss = false` with a message from the
  resources, or throw a `KnownException` subclass (§8).
* Validate phone numbers, amounts and IDs at the boundary. Do not trust the client for prices,
  amounts or payment status (plans 011 and 012).

## 7.4 Pagination and list endpoints

* Use `GetListQueryParams` (`src/Logiwall.Shared/Dtos/Global/GetListQueryParams.cs`) for paging,
  filters and sorting, and `PagedResult<T>` (`src/Logiwall.Shared/Dtos/PagedResultDto.cs`) for
  the response.
* Cap the page size on the server. Apply a stable order before `Skip`/`Take`.
* Allow-list the fields a client can filter or sort by. Never build a query from a raw field name
  (plan 014).
