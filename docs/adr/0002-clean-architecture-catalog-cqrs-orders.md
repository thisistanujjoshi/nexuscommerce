# ADR 0002 — Clean Architecture for Catalog, CQRS + Mediator for Orders

**Status:** Accepted
**Date:** 2026-07-22

## Context

Both C# services need clear internal structure, but using the identical pattern twice demonstrates less range and hides the trade-offs between the two approaches.

## Decision

- **Catalog** uses Clean Architecture with the Repository pattern: `Domain` (entities, no dependencies) ← `Application` (use cases, repository interfaces) ← `Infrastructure` (EF Core, repository implementations) ← `Api` (controllers, DI wiring). Suited to straightforward CRUD-plus-search.
- **Orders** uses CQRS with a Mediator: commands (place order, cancel) are separated from queries (order history, order detail), each handled by a dedicated handler. Suited to a workflow-heavy domain where writes carry business rules and reads want flat, fast projections.

## Consequences

- Two idiomatic reference implementations exist side by side for comparison.
- CQRS adds ceremony for simple operations — accepted in Orders only, where the domain justifies it.
- Repository pattern in Catalog keeps EF Core out of the domain, at the cost of some indirection.
