# ADR 0003 — Event-driven notifications with pluggable transport

**Status:** Accepted
**Date:** 2026-07-22

## Context

Order lifecycle changes must reach customers without coupling the Orders service to
notification concerns (email templating, delivery, retry). The portfolio plan calls for a
Python/FastAPI Notifications service consuming events off a message queue — but local
development on this machine has no Docker yet, so RabbitMQ and MongoDB cannot be assumed.

## Decision

1. **Envelope contract.** Every integration event is camelCase JSON:
   `{ eventType, occurredAtUtc, data }`, published to the durable topic exchange
   `nexus.events` with the event type as routing key (`order.placed`,
   `order.status-changed`). The Notifications queue binds `order.*`.

2. **Pluggable transport on both sides.**
   - Orders (producer): `IEventPublisher` with `RabbitMq`, `Http`, and `None`
     implementations selected by `EventBus:Transport`. The Http publisher POSTs the
     envelope directly to the consumer's ingress and exists purely so the event flow
     runs end-to-end in local dev without a broker.
   - Notifications (consumer): `NOTIF_TRANSPORT=rabbitmq` starts an aio-pika consumer
     with reconnect/backoff; `http` exposes `POST /api/v1/events` instead.
   Storage follows the same pattern: `NOTIF_STORE=mongodb` (motor) or `memory`.

3. **At-most-once delivery, deliberately.** Publishers catch transport failures, log a
   warning, and let the business operation succeed — an unreachable notifier must not
   block order placement. The upgrade path (transactional outbox + publisher retries)
   is documented future work for the Kubernetes phase, where RabbitMQ becomes the
   default transport in both services' configuration.

## Consequences

- The complete browse → order → notification flow runs on a bare machine today, and
  switching to broker + Mongo in Phase 5 is configuration, not code.
- Dev (HTTP) and prod (RabbitMQ) transports can drift in behavior — mitigated by
  keeping both paths converging on the same `handle_event` dispatch and envelope model.
- Lost events are possible until the outbox lands; acceptable for notifications,
  and stated openly rather than hidden.
