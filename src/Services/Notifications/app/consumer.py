import asyncio
import logging

from .domain import EventEnvelope
from .store import NotificationStore

logger = logging.getLogger("notifications.consumer")


class RabbitMqConsumer:
    """Consumes order.* events from the nexus.events topic exchange.

    Selected with NOTIF_TRANSPORT=rabbitmq. Reconnects with backoff so the
    service can start before the broker does (e.g. during compose startup).
    """

    def __init__(
        self,
        amqp_url: str,
        exchange: str,
        queue: str,
        store: NotificationStore,
    ) -> None:
        self._amqp_url = amqp_url
        self._exchange = exchange
        self._queue = queue
        self._store = store
        self._task: asyncio.Task | None = None

    def start(self) -> None:
        self._task = asyncio.create_task(self._run(), name="rabbitmq-consumer")

    async def stop(self) -> None:
        if self._task:
            self._task.cancel()
            try:
                await self._task
            except asyncio.CancelledError:
                pass

    async def _run(self) -> None:
        import aio_pika

        delay = 1.0
        while True:
            try:
                connection = await aio_pika.connect_robust(self._amqp_url)
                async with connection:
                    channel = await connection.channel()
                    await channel.set_qos(prefetch_count=16)

                    exchange = await channel.declare_exchange(
                        self._exchange, aio_pika.ExchangeType.TOPIC, durable=True
                    )
                    queue = await channel.declare_queue(self._queue, durable=True)
                    await queue.bind(exchange, routing_key="order.*")

                    logger.info(
                        "Consuming %s (routing order.*) on %s", self._queue, self._exchange
                    )
                    delay = 1.0

                    async with queue.iterator() as messages:
                        async for message in messages:
                            async with message.process(requeue=False):
                                await self._dispatch(message.body)
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Consumer error; reconnecting in %.0fs", delay)
                await asyncio.sleep(delay)
                delay = min(delay * 2, 30.0)

    async def _dispatch(self, body: bytes) -> None:
        from .handlers import handle_event

        try:
            event = EventEnvelope.model_validate_json(body)
        except ValueError:
            logger.warning("Dropping malformed event payload: %.200s", body)
            return
        await handle_event(event, self._store)
