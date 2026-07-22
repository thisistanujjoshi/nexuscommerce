import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { placeOrder } from '../api/orders'
import { useCart } from '../cart/CartContext'
import { cartTotal } from '../cart/cartReducer'
import { rememberOrder } from '../orders/orderHistory'

/**
 * Demo stand-in for an authenticated customer — a stable per-browser id,
 * replaced by real identity once the JWT gateway lands in Phase 4.
 */
function customerId(): string {
  const key = 'nexuscommerce.customerId'
  let id = localStorage.getItem(key)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(key, id)
  }
  return id
}

export default function CartPage() {
  const { cart, dispatch } = useCart()
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  const total = cartTotal(cart)

  async function checkout(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const order = await placeOrder(
        customerId(),
        email,
        cart.lines.map((l) => ({
          productId: l.productId,
          productName: l.name,
          unitPrice: l.unitPrice,
          quantity: l.quantity,
        })),
      )
      rememberOrder(order.id)
      dispatch({ type: 'clear' })
      navigate(`/orders?placed=${order.id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Something went wrong placing the order.')
    } finally {
      setSubmitting(false)
    }
  }

  if (cart.lines.length === 0) {
    return <p className="muted">Your cart is empty — head to the shop to add something.</p>
  }

  return (
    <section className="cart">
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Price</th>
            <th>Qty</th>
            <th>Line total</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {cart.lines.map((line) => (
            <tr key={line.productId}>
              <td>
                {line.name} <span className="sku">{line.sku}</span>
              </td>
              <td>${line.unitPrice.toFixed(2)}</td>
              <td>
                <input
                  type="number"
                  min={0}
                  max={line.availableStock}
                  value={line.quantity}
                  onChange={(e) =>
                    dispatch({
                      type: 'setQuantity',
                      productId: line.productId,
                      quantity: Number(e.target.value),
                    })
                  }
                  aria-label={`Quantity for ${line.name}`}
                />
              </td>
              <td>${(line.unitPrice * line.quantity).toFixed(2)}</td>
              <td>
                <button
                  className="link"
                  onClick={() => dispatch({ type: 'remove', productId: line.productId })}
                >
                  Remove
                </button>
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={3}>Total</td>
            <td colSpan={2} className="price">
              ${total.toFixed(2)}
            </td>
          </tr>
        </tfoot>
      </table>

      <form onSubmit={checkout} className="checkout">
        <label htmlFor="email">Email for order updates</label>
        <input
          id="email"
          type="email"
          required
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Placing order…' : `Place order — $${total.toFixed(2)}`}
        </button>
      </form>
    </section>
  )
}
