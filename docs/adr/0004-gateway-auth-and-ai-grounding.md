# ADR 0004 — YARP gateway with JWT auth; catalog-grounded AI chat

**Status:** Accepted
**Date:** 2026-07-22

## Context

Phase 4 adds the two remaining runtime services: a single public entry point that
enforces authentication/authorization, and a product Q&A chatbot. Both need choices
that hold up in interviews: where auth lives, how roles map to routes, and how the
LLM is grounded without hallucinating products.

## Decision

1. **YARP reverse proxy (ASP.NET Core) as the gateway** on port 5100. Routes are
   configuration, not code: `/catalog`, `/orders`, `/notifications`, `/ai` each strip
   their prefix and forward to the owning service. Auth is evaluated by standard
   ASP.NET Core authorization policies *before* proxying.

2. **JWT with role-based policies.** The gateway issues demo tokens itself
   (`POST /auth/token`, users in configuration) — a stand-in for a real identity
   provider; validation middleware would be unchanged with Entra ID/Auth0/Keycloak.
   Policy map: catalog reads and AI chat are anonymous, orders require any
   authenticated user, catalog writes and notifications require the `admin` role.
   Catalog read-vs-write is split by HTTP method at the route level.

3. **AI grounding by full-catalog context.** The AiSupport service fetches the entire
   catalog from the Catalog API per question and passes it in the system prompt; the
   model is instructed to answer only from that context. With a demo-sized catalog
   this is simpler and strictly more accurate than retrieval. At real scale this
   becomes a retrieval step (catalog search endpoint or embeddings) — the
   `CatalogClient.get_context` seam is where that swap happens.

4. **Pluggable LLM client.** `AISUPPORT_LLM=anthropic` uses the Claude API via the
   official SDK (model configurable, default `claude-opus-4-8`); `stub` is a
   deterministic offline client so dev and CI need no API key — the same
   provider-switch pattern used for databases (ADR 0001) and messaging (ADR 0003).
   The API key is read from the environment only, never configuration files.

## Consequences

- Services stay auth-agnostic; the trust boundary is the gateway. (Direct service
  ports remain open for local dev; in the Kubernetes deployment only the gateway is
  exposed.)
- Demo credentials and a dev signing key live in `appsettings.json` — acceptable for
  a portfolio, called out so reviewers see it's a conscious choice; real deployments
  override via environment/Key Vault.
- Per-question catalog fetch adds a hop but guarantees fresh stock/prices in answers;
  a cache with short TTL is the obvious optimization if latency ever matters.
