# ADR 0005 — Containerization, Kubernetes packaging, and CI/CD

**Status:** Accepted
**Date:** 2026-07-22

## Context

Phase 5 makes the platform deployable: every service containerised, packaged
for Kubernetes, provisioned on cloud infrastructure, and shipped by automated
pipelines. The choices below favour a single coherent story over breadth.

## Decision

1. **Multi-stage Docker images, one per service.** .NET services restore in a
   cached layer, publish in an SDK stage, and run from the slim aspnet runtime
   as a non-root user (curl added only for the healthcheck). Python services run
   on `python:3.12-slim` as non-root. Frontends build with Node then ship as
   static files behind nginx with SPA fallback routing. Images land at
   73–496 MB.

2. **docker-compose for the whole platform locally.** One `docker compose up`
   runs the gateway, five services, two frontends, and a real
   PostgreSQL/RabbitMQ/MongoDB backbone with health-gated startup — the same
   provider/transport switches the services already support (ADR 0001, 0003)
   selected via environment variables. Verified end-to-end: an order placed
   through the containerised gateway flows Orders→RabbitMQ→Notifications→Mongo.

3. **A single Helm umbrella chart, data-driven.** The deployment and service
   templates range over a `.Values.services` map, so adding a service is a
   values edit, not a new template. Per-environment values files
   (dev/staging/prod) scale replicas and flip Catalog's database provider.
   Secrets render into a Kubernetes Secret for local use; production sources
   them from Key Vault via the Secrets Store CSI driver. Only the gateway is
   exposed, through an Ingress.

4. **Terraform for Azure infrastructure.** AKS, VNet, Azure Container Registry
   (AKS pulls via its managed identity, no stored credential), Key Vault, and a
   managed PostgreSQL flexible server. `environment` drives naming and sizing.
   State backend and secrets are kept out of the repo.

5. **Three CI/CD pipelines, deliberately.** GitHub Actions is the working CI
   (builds and tests every stack, lints/renders the Helm chart) plus a
   tag-triggered CD that pushes images and runs `helm upgrade`. Azure DevOps and
   a Jenkinsfile mirror the same build → scan → deploy shape — the plan's
   "two pipelines, one outcome" comparison, and the seam OpsForge documents.

## Consequences

- The platform runs identically on a laptop (compose) and a cluster (Helm),
  because the runtime differences are all configuration the services already
  read from the environment.
- Terraform and the Azure DevOps/Jenkins pipelines are written and syntax-checked
  but not applied/executed here — applying needs an Azure subscription and
  credentials, which is a deliberate, defensible boundary for a portfolio.
- Three pipelines is redundant for one team, but demonstrating all three is the
  point; a real project would pick one.
