# ADR 0001 — One service per bounded context, with polyglot persistence

**Status:** Accepted
**Date:** 2026-07-22

## Context

NexusCommerce needs to demonstrate distributed-systems architecture with independently deployable services. A single shared database would couple the services and undermine the microservices story.

## Decision

Each bounded context (Catalog, Orders, Notifications) is a separate service owning its own data store:

- **Catalog** → SQL Server: relational product/category data, read-heavy.
- **Orders** → PostgreSQL: transactional order workflow, CQRS separates the write model from read models.
- **Notifications** → MongoDB: schema-flexible notification log, written from queue events.

Services never read each other's databases; they communicate over REST (synchronous) or the message queue (asynchronous events).

## Consequences

- Independent deployability and schema evolution per service.
- Cross-service queries require API composition at the gateway or duplication via events — accepted cost.
- Local development needs multiple databases; mitigated later with Docker Compose (Phase 5).
