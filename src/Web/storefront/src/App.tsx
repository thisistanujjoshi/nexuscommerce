import { Link, NavLink, Route, Routes } from 'react-router-dom'
import { useCart } from './cart/CartContext'
import { cartCount } from './cart/cartReducer'
import CartPage from './pages/CartPage'
import OrdersPage from './pages/OrdersPage'
import ProductsPage from './pages/ProductsPage'

export default function App() {
  const { cart } = useCart()
  const count = cartCount(cart)

  return (
    <div className="layout">
      <header className="site-header">
        <Link to="/" className="brand">
          Nexus<span>Commerce</span>
        </Link>
        <nav>
          <NavLink to="/" end>
            Shop
          </NavLink>
          <NavLink to="/orders">Orders</NavLink>
          <NavLink to="/cart" className="cart-link">
            Cart{count > 0 && <span className="badge">{count}</span>}
          </NavLink>
        </nav>
      </header>

      <main>
        <Routes>
          <Route path="/" element={<ProductsPage />} />
          <Route path="/cart" element={<CartPage />} />
          <Route path="/orders" element={<OrdersPage />} />
        </Routes>
      </main>

      <footer className="site-footer">
        NexusCommerce — portfolio demo storefront. React + TypeScript, talking to the Catalog &
        Orders microservices.
      </footer>
    </div>
  )
}
