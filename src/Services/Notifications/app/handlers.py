import logging

from .domain import EventEnvelope, Notification
from .store import NotificationStore

logger = logging.getLogger("notifications.handlers")


def _order_ref(order_id: str | None) -> str:
    return f"#{order_id[:8]}" if order_id else "(unknown)"


def _render_order_placed(data: dict) -> tuple[str, str]:
    order_ref = _order_ref(data.get("orderId"))
    total = data.get("total", 0)
    lines = [
        f"Thanks for your order {order_ref}!",
        "",
        "Items:",
    ]
    for item in data.get("items", []):
        lines.append(f"  - {item.get('quantity', 1)} x {item.get('productName', 'item')}")
    lines += ["", f"Order total: ${total:.2f}", "", "We'll let you know when it ships."]
    return f"Order {order_ref} received — ${total:.2f}", "\n".join(lines)


def _render_status_changed(data: dict) -> tuple[str, str]:
    order_ref = _order_ref(data.get("orderId"))
    new_status = data.get("newStatus", "updated")
    subject = f"Order {order_ref} is now {new_status}"
    body = (
        f"Hi,\n\nYour order {order_ref} moved from "
        f"{data.get('oldStatus', 'its previous status')} to {new_status}.\n\n"
        "NexusCommerce"
    )
    return subject, body


async def handle_event(event: EventEnvelope, store: NotificationStore) -> Notification | None:
    """Turn a domain event into a persisted (stub-sent) notification.

    Unknown event types are ignored deliberately: the topic binding may deliver
    more order.* events than this service cares about.
    """
    data = event.data
    email = data.get("customerEmail")
    if not email:
        logger.warning("Event %s has no customerEmail; skipping", event.event_type)
        return None

    if event.event_type == "order.placed":
        subject, body = _render_order_placed(data)
    elif event.event_type == "order.status-changed":
        subject, body = _render_status_changed(data)
    else:
        logger.info("Ignoring unhandled event type %s", event.event_type)
        return None

    notification = Notification(
        to_email=email,
        subject=subject,
        body=body,
        event_type=event.event_type,
        order_id=data.get("orderId"),
    )

    # "Send" the email: this demo stops at structured logging; a real deployment
    # would hand off to an SMTP relay or provider API here.
    logger.info("EMAIL to=%s subject=%r", notification.to_email, notification.subject)

    await store.add(notification)
    return notification
