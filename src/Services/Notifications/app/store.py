from typing import Protocol

from .domain import Notification


class NotificationStore(Protocol):
    async def add(self, notification: Notification) -> None: ...

    async def list_by_email(self, email: str, limit: int = 50) -> list[Notification]: ...

    async def list_recent(self, limit: int = 50) -> list[Notification]: ...


class InMemoryStore:
    """Dev/test store; newest-first, process-local."""

    def __init__(self) -> None:
        self._items: list[Notification] = []

    async def add(self, notification: Notification) -> None:
        self._items.append(notification)

    async def list_by_email(self, email: str, limit: int = 50) -> list[Notification]:
        matches = [n for n in self._items if n.to_email.lower() == email.lower()]
        return sorted(matches, key=lambda n: n.created_at_utc, reverse=True)[:limit]

    async def list_recent(self, limit: int = 50) -> list[Notification]:
        return sorted(self._items, key=lambda n: n.created_at_utc, reverse=True)[:limit]


class MongoStore:
    """MongoDB-backed store (motor). Selected with NOTIF_STORE=mongodb."""

    def __init__(self, url: str, database: str) -> None:
        from motor.motor_asyncio import AsyncIOMotorClient

        self._client = AsyncIOMotorClient(url)
        self._collection = self._client[database]["notifications"]

    async def add(self, notification: Notification) -> None:
        await self._collection.insert_one(notification.model_dump())

    async def list_by_email(self, email: str, limit: int = 50) -> list[Notification]:
        cursor = (
            self._collection.find({"to_email": email}, {"_id": False})
            .sort("created_at_utc", -1)
            .limit(limit)
        )
        return [Notification(**doc) async for doc in cursor]

    async def list_recent(self, limit: int = 50) -> list[Notification]:
        cursor = (
            self._collection.find({}, {"_id": False})
            .sort("created_at_utc", -1)
            .limit(limit)
        )
        return [Notification(**doc) async for doc in cursor]


def create_store(store_kind: str, mongo_url: str, mongo_db: str) -> NotificationStore:
    if store_kind == "mongodb":
        return MongoStore(mongo_url, mongo_db)
    if store_kind == "memory":
        return InMemoryStore()
    raise ValueError(f"Unknown store '{store_kind}'")
