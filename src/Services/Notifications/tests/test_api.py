from datetime import datetime, timezone

import httpx

from app.config import Settings
from app.main import create_app
from app.store import InMemoryStore


def make_client() -> httpx.AsyncClient:
    app = create_app(
        config=Settings(transport="http", store="memory"),
        store=InMemoryStore(),
    )
    transport = httpx.ASGITransport(app=app)
    return httpx.AsyncClient(transport=transport, base_url="http://test")


def placed_event(email: str = "buyer@example.com") -> dict:
    return {
        "eventType": "order.placed",
        "occurredAtUtc": datetime.now(timezone.utc).isoformat(),
        "data": {
            "orderId": "aaaabbbb-0000-0000-0000-000000000000",
            "customerEmail": email,
            "total": 99.5,
            "items": [{"productName": "Widget", "quantity": 2}],
        },
    }


async def test_health():
    async with make_client() as client:
        response = await client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "Healthy"


async def test_event_roundtrip_creates_queryable_notification():
    async with make_client() as client:
        post = await client.post("/api/v1/events", json=placed_event())
        assert post.status_code == 202
        assert post.json()["handled"] is True

        listed = await client.get("/api/v1/notifications", params={"email": "buyer@example.com"})

    assert listed.status_code == 200
    items = listed.json()
    assert len(items) == 1
    assert items[0]["toEmail" if "toEmail" in items[0] else "to_email"] == "buyer@example.com"
    assert "$99.50" in items[0]["subject"]


async def test_notifications_filtered_by_email():
    async with make_client() as client:
        await client.post("/api/v1/events", json=placed_event("a@example.com"))
        await client.post("/api/v1/events", json=placed_event("b@example.com"))

        only_a = await client.get("/api/v1/notifications", params={"email": "a@example.com"})
        everyone = await client.get("/api/v1/notifications")

    assert len(only_a.json()) == 1
    assert len(everyone.json()) == 2


async def test_unhandled_event_type_is_accepted_but_not_stored():
    event = placed_event()
    event["eventType"] = "order.audited"

    async with make_client() as client:
        post = await client.post("/api/v1/events", json=event)
        listed = await client.get("/api/v1/notifications")

    assert post.status_code == 202
    assert post.json()["handled"] is False
    assert listed.json() == []


async def test_malformed_event_is_rejected():
    async with make_client() as client:
        response = await client.post("/api/v1/events", json={"nope": True})

    assert response.status_code == 422
