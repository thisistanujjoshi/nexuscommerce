import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, Query, Response, status

from .config import Settings, settings
from .consumer import RabbitMqConsumer
from .domain import EventEnvelope, Notification
from .handlers import handle_event
from .store import NotificationStore, create_store

logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")
logger = logging.getLogger("notifications")


def create_app(config: Settings | None = None, store: NotificationStore | None = None) -> FastAPI:
    """App factory; tests inject their own Settings/store."""
    config = config or settings
    store = store or create_store(config.store, config.mongo_url, config.mongo_db)
    consumer: RabbitMqConsumer | None = None

    @asynccontextmanager
    async def lifespan(_: FastAPI):
        nonlocal consumer
        if config.transport == "rabbitmq":
            consumer = RabbitMqConsumer(
                config.amqp_url, config.amqp_exchange, config.amqp_queue, store
            )
            consumer.start()
            logger.info("Transport: RabbitMQ (%s)", config.amqp_exchange)
        else:
            logger.info("Transport: HTTP (POST /api/v1/events)")
        yield
        if consumer:
            await consumer.stop()

    app = FastAPI(
        title="NexusCommerce Notifications API",
        version="1.0",
        description=(
            "Consumes order events and records (stub-)sent notifications — "
            "part of the NexusCommerce distributed order-management platform."
        ),
        lifespan=lifespan,
    )

    # Prometheus metrics at /metrics (scraped by the OpsForge observability stack).
    from prometheus_fastapi_instrumentator import Instrumentator

    Instrumentator(excluded_handlers=["/metrics", "/health"]).instrument(app).expose(app)

    @app.get("/health")
    async def health() -> dict:
        return {"status": "Healthy", "transport": config.transport, "store": config.store}

    @app.post(
        "/api/v1/events",
        status_code=status.HTTP_202_ACCEPTED,
        summary="Ingest an event (HTTP transport)",
        description=(
            "Dev-mode ingress used when no message broker is available; "
            "the RabbitMQ consumer replaces this in broker-backed environments."
        ),
    )
    async def ingest_event(event: EventEnvelope, response: Response) -> dict:
        notification = await handle_event(event, store)
        if notification is None:
            return {"handled": False, "reason": f"no handler for '{event.event_type}'"}
        return {"handled": True, "notificationId": notification.id}

    @app.get("/api/v1/notifications", response_model=list[Notification])
    async def list_notifications(
        email: str | None = Query(default=None, description="Filter by recipient email"),
        limit: int = Query(default=50, ge=1, le=200),
    ) -> list[Notification]:
        if email:
            return await store.list_by_email(email, limit)
        return await store.list_recent(limit)

    return app


app = create_app()
