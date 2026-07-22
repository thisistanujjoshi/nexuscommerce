from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Service configuration, overridable via NOTIF_* environment variables.

    transport: how order events arrive.
        "http"     - events are POSTed to /api/v1/events (local dev, no broker needed)
        "rabbitmq" - events are consumed from the nexus.events topic exchange
    store: where notifications are persisted.
        "memory"  - in-process store (local dev / tests)
        "mongodb" - MongoDB via motor
    """

    transport: str = "http"
    store: str = "memory"

    amqp_url: str = "amqp://guest:guest@localhost:5672/"
    amqp_exchange: str = "nexus.events"
    amqp_queue: str = "notifications"

    mongo_url: str = "mongodb://localhost:27017"
    mongo_db: str = "nexuscommerce_notifications"

    model_config = {"env_prefix": "NOTIF_"}


settings = Settings()
