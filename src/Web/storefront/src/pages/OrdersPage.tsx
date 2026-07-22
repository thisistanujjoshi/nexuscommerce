import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { getOrder } from '../api/orders'
import type { Order } from '../api/types'
import { knownOrderIds } from '../orders/orderHistory'

const STATUS_LABELS: Record<Order['status'], string> = {
  Pending: '🕐 Pending',
  Confirmed: '✅ Confirmed',
  Shipped: '🚚 Shipped',
  Delivered: '📦 Delivered',
  Cancelled: '❌ Cancelled',
}

export default function OrdersPage() {
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [params] = useSearchParams()
  const justPlaced = params.get('placed')

  useEffect(() => {
    const ids = knownOrderIds()
    if (ids.length === 0) {
      setLoading(false)
      return
    }
    Promise.allSettled(ids.map(getOrder)).then((results) => {
      setOrders(
        results
          .filter((r): r is PromiseFulfilledResult<Order> => r.status === 'fulfilled')
          .map((r) => r.value),
      )
      setLoading(false)
    })
  }, [])

  if (loading) return <p className="muted">Loading your orders…</p>
  if (orders.length === 0) return <p className="muted">No orders yet.</p>

  return (
    <section>
      {justPlaced && <p className="success">Order placed! A confirmation is on its way.</p>}
      {orders.map((order) => (
        <article key={order.id} className={`order-card ${order.id === justPlaced ? 'highlight' : ''}`}>
          <header>
            <span className="order-id">#{order.id.slice(0, 8)}</span>
            <span className="status">{STATUS_LABELS[order.status]}</span>
            <time dateTime={order.placedAtUtc}>
              {new Date(order.placedAtUtc).toLocaleString()}
            </time>
          </header>
          <ul>
            {order.items.map((item) => (
              <li key={item.productId}>
                {item.quantity} × {item.productName} — ${item.lineTotal.toFixed(2)}
              </li>
            ))}
          </ul>
          <footer className="price">Total: ${order.total.toFixed(2)}</footer>
        </article>
      ))}
    </section>
  )
}
