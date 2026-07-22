from datetime import datetime, timezone

from app.domain import EventEnvelope
from app.handlers import handle_event
from app.store import InMemoryStore


def envelope(event_type: str, data: dict) -> EventEnvelope:
    return EventEnvelope(
        eventType=event_type,
        occurredAtUtc=datetime.now(timezone.utc),
        data=data,
    )


async def test_order_placed_creates_notification():
    store = InMemoryStore()
    event = envelope(
        "order.placed",
        {
            "orderId": "566a7b7d-0000-0000-0000-000000000000",
            "customerEmail": "buyer@example.com",
            "total": 410.99,
            "items": [
                {"productName": "27\" 4K Monitor", "quantity": 1},
                {"productName": "Clean Architecture", "quantity": 1},
            ],
        },
    )

    notification = await handle_event(event, store)

    assert notification is not None
    assert notification.to_email == "buyer@example.com"
    assert "#566a7b7d" in notification.subject
    assert "$410.99" in notification.subject
    assert "27\" 4K Monitor" in notification.body
    assert (await store.list_by_email("buyer@example.com"))[0].id == notification.id


async def test_status_changed_creates_notification():
    store = InMemoryStore()
    event = envelope(
        "order.status-changed",
        {
            "orderId": "566a7b7d-0000-0000-0000-000000000000",
            "customerEmail": "buyer@example.com",
            "oldStatus": "Pending",
            "newStatus": "Confirmed",
        },
    )

    notification = await handle_event(event, store)

    assert notification is not None
    assert "Confirmed" in notification.subject
    assert notification.event_type == "order.status-changed"


async def test_unknown_event_type_is_ignored():
    store = InMemoryStore()
    event = envelope("order.exploded", {"customerEmail": "buyer@example.com"})

    assert await handle_event(event, store) is None
    assert await store.list_recent() == []


async def test_event_without_email_is_skipped():
    store = InMemoryStore()
    event = envelope("order.placed", {"orderId": "x", "total": 1.0})

    assert await handle_event(event, store) is None
    assert await store.list_recent() == []
