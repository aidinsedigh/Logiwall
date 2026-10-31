# ADR-0002: Keep the layered architecture

**Status:** Accepted, 2026-10-27

## Decision

Logiwall API keeps its existing layered projects (`Server`, `Application`, `Infrastructure`,
`Domain`, `Shared`), its MVC controllers and its `ServiceResponse<T>` contract.

It does not move to:

* a modular monolith with one project per module
* Minimal APIs, MediatR or CQRS
* an exception-only error pattern (Logiwall keeps `ServiceResponse<T>` and `KnownException`)

## Context

The React front end (`Logiwall-Front`) depends on the `api/[controller]/[action]` routes and on
the `ServiceResponse<T>` shape, including the misspelled `Succuss` property. The audit plans
in `docs/archive/cycles/001-*` to `016-*` were written against the current layers and files.

## Consequences

* A rewrite to a different architecture would break the front-end contract and every open audit
  plan. It is out of scope.
* Rules in `docs/conventions/` describe Logiwall's real code. They contain no rules for features
  Logiwall does not have (integration events, gRPC, outbox).
* A future architecture change needs a new ADR that supersedes this one.
