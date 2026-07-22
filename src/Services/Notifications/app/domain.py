from datetime import datetime, timezone
from typing import Any, Literal
from uuid import uuid4

from pydantic import BaseModel, Field


class EventEnvelope(BaseModel):
    """Envelope shared by every event on the nexus.events exchange.

    The Orders service (C#) publishes the same shape with camelCase keys.
    """

    event_type: str = Field(alias="eventType")
    occurred_at_utc: datetime = Field(alias="occurredAtUtc")
    data: dict[str, Any]

    model_config = {"populate_by_name": True}


class Notification(BaseModel):
    id: str = Field(default_factory=lambda: str(uuid4()))
    to_email: str
    subject: str
    body: str
    event_type: str
    order_id: str | None = None
    channel: Literal["email"] = "email"
    status: Literal["sent"] = "sent"
    created_at_utc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
