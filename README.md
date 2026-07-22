# NexusCommerce

A distributed order-management platform — a mini e-commerce backend split into independently deployable microservices, an API gateway, and two frontends.

## Architecture

| Service | Stack | Data store | Patterns |
|---|---|---|---|
| **Catalog** | ASP.NET Core Web API (C#) | SQL Server | Clean Architecture, Repository |
| **Orders** | ASP.NET Core Web API (C#) | PostgreSQL | CQRS + Mediator |
| **Notifications** | Python (FastAPI) | MongoDB | Event-driven (RabbitMQ) |
| **AI Support** | Python + LLM API | — | RAG over catalog data |
| **Gateway** | API gateway | — | JWT auth, routing |
| **Storefront** | React + TypeScript | — | SPA |
| **Admin** | Vue.js | — | Inventory dashboard |

Every service exposes REST endpoints documented with OpenAPI/Swagger.

## Repository layout

```
src/
  Services/
    Catalog/        # Catalog.Api, Catalog.Application, Catalog.Domain, Catalog.Infrastructure
    Orders/         # Orders.Api, Orders.Application, Orders.Domain, Orders.Infrastructure
tests/              # xUnit + Moq unit tests per service
docs/               # Architecture notes and ADRs
```

## Roadmap

- [ ] **Phase 1** — Catalog + Orders services, database schemas, unit tests
- [ ] **Phase 2** — React storefront + Vue admin dashboard
- [ ] **Phase 3** — Notifications service, message queue, event-driven integration
- [ ] **Phase 4** — AI support chatbot, API gateway with JWT auth
- [ ] **Phase 5** — Docker, Kubernetes + Helm, Terraform, CI/CD pipelines

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

In the `Development` environment the services run on SQLite (zero setup, auto-seeded demo data); the `SqlServer`/`PostgreSQL` providers are selected via `Database:Provider` in configuration for real environments.

```bash
dotnet build
dotnet test
```

Run the Catalog API:

```bash
dotnet run --project src/Services/Catalog/Catalog.Api
```

Swagger UI is available at `/swagger` in development.
