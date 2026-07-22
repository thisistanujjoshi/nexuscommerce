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
  Gateway/
    Gateway.Api/    # YARP reverse proxy + JWT auth (single public entry point)
  Services/
    Catalog/        # Catalog.Api, Catalog.Application, Catalog.Domain, Catalog.Infrastructure
    Orders/         # Orders.Api, Orders.Application, Orders.Domain, Orders.Infrastructure
    Notifications/  # Python FastAPI event consumer (app/ + pytest tests/)
    AiSupport/      # Python FastAPI product Q&A chatbot (Claude API, catalog-grounded)
  Web/
    storefront/     # React + TypeScript customer storefront (Vite)
    admin/          # Vue 3 + TypeScript admin dashboard (Vite)
tests/              # xUnit + Moq unit tests per service
docs/               # Architecture notes and ADRs
```

## Roadmap

- [x] **Phase 1** — Catalog + Orders services, database schemas, unit tests
- [x] **Phase 2** — React storefront + Vue admin dashboard
- [x] **Phase 3** — Notifications service, message queue, event-driven integration
- [x] **Phase 4** — AI support chatbot, API gateway with JWT auth
- [ ] **Phase 5** — Docker, Kubernetes + Helm, Terraform, CI/CD pipelines

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

In the `Development` environment the services run on SQLite (zero setup, auto-seeded demo data); the `SqlServer`/`PostgreSQL` providers are selected via `Database:Provider` in configuration for real environments.

```bash
dotnet build
dotnet test
```

Run the APIs:

```bash
dotnet run --project src/Services/Catalog/Catalog.Api   # http://localhost:5101
dotnet run --project src/Services/Orders/Orders.Api     # http://localhost:5102
```

Swagger UI is available at `/swagger` on each API in development.

Run the Notifications service (Python 3.12+):

```bash
cd src/Services/Notifications
python -m venv .venv && .venv/Scripts/pip install -e ".[dev]"   # Scripts -> bin on Linux/macOS
.venv/Scripts/python -m pytest                                   # run its tests
.venv/Scripts/python -m uvicorn app.main:app --port 5103         # http://localhost:5103 (docs at /docs)
```

In development, Orders publishes `order.placed` / `order.status-changed` events over HTTP
straight to the Notifications ingress; with a broker available both sides switch to
RabbitMQ via configuration (`EventBus:Transport` / `NOTIF_TRANSPORT`) — see ADR 0003.

Run the AI Support chatbot (Python 3.12+, same venv steps as Notifications):

```bash
cd src/Services/AiSupport
.venv/Scripts/python -m uvicorn app.main:app --port 5104   # http://localhost:5104 (docs at /docs)
```

Set `ANTHROPIC_API_KEY` for real answers (model: `AISUPPORT_MODEL`, default
`claude-opus-4-8`), or `AISUPPORT_LLM=stub` for a deterministic offline mode.

Run the API gateway:

```bash
dotnet run --project src/Gateway/Gateway.Api                # http://localhost:5100
```

The gateway is the single public entry point. Get a token, then call any service
through it (`/catalog`, `/orders`, `/notifications`, `/ai` + the service's own path):

```bash
curl -X POST http://localhost:5100/auth/token -H "Content-Type: application/json" \
  -d '{"username":"demo","password":"demo123"}'
curl http://localhost:5100/orders/api/v1/orders -H "Authorization: Bearer <token>"
```

Demo users: `admin/admin123` (admin role), `demo/demo123` (customer). Catalog reads and
AI chat are anonymous; orders require a token; catalog writes and notifications require
the admin role — see ADR 0004.

## Local infrastructure (Docker)

`infra/docker-compose.dev.yml` provides the backing services with dev-only credentials,
grouped into profiles so you only pay the RAM for what you're using:

```bash
docker compose -f infra/docker-compose.dev.yml --profile broker up -d     # RabbitMQ + MongoDB
docker compose -f infra/docker-compose.dev.yml --profile databases up -d  # SQL Server + PostgreSQL
docker compose -f infra/docker-compose.dev.yml --profile all up -d        # everything
```

With the broker profile running, switch the event flow onto RabbitMQ:

- Orders: set `EventBus:Transport` to `RabbitMq` (appsettings or env var)
- Notifications: run with `NOTIF_TRANSPORT=rabbitmq` (and `NOTIF_STORE=mongodb` for Mongo)

The RabbitMQ management UI is at http://localhost:15672 (guest/guest).

Run the frontends (Node 20+):

```bash
cd src/Web/storefront && npm install && npm run dev     # http://localhost:5173
cd src/Web/admin && npm install && npm run dev          # http://localhost:5174
```

The dev servers point at the API ports above by default; override with
`VITE_CATALOG_API` / `VITE_ORDERS_API` env vars. Storefront unit tests: `npm test`.
