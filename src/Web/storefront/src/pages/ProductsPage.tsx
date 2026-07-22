import { useEffect, useState } from 'react'
import { getCategories, searchProducts } from '../api/catalog'
import type { Category, Product } from '../api/types'
import { useCart } from '../cart/CartContext'

export default function ProductsPage() {
  const [categories, setCategories] = useState<Category[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const { cart, dispatch } = useCart()

  useEffect(() => {
    getCategories().then(setCategories).catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    const timer = setTimeout(() => {
      searchProducts({ search: search || undefined, categoryId: categoryId || undefined })
        .then((result) => {
          if (!cancelled) {
            setProducts(result.items)
            setError(null)
          }
        })
        .catch((e: Error) => {
          if (!cancelled) setError(e.message)
        })
        .finally(() => {
          if (!cancelled) setLoading(false)
        })
    }, 250)
    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [search, categoryId])

  const inCart = (productId: string) =>
    cart.lines.find((l) => l.productId === productId)?.quantity ?? 0

  return (
    <section>
      <div className="toolbar">
        <input
          type="search"
          placeholder="Search products…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Search products"
        />
        <select
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
          aria-label="Filter by category"
        >
          <option value="">All categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="error">Could not load products: {error}</p>}
      {loading && !error && <p className="muted">Loading…</p>}
      {!loading && !error && products.length === 0 && (
        <p className="muted">No products match your search.</p>
      )}

      <div className="product-grid">
        {products.map((product) => {
          const reserved = inCart(product.id)
          const remaining = product.stockQuantity - reserved
          return (
            <article key={product.id} className="product-card">
              <header>
                <h3>{product.name}</h3>
                <span className="sku">{product.sku}</span>
              </header>
              <p className="description">{product.description}</p>
              <footer>
                <span className="price">${product.price.toFixed(2)}</span>
                <button
                  disabled={remaining < 1}
                  onClick={() => dispatch({ type: 'add', product })}
                >
                  {remaining < 1 ? 'Out of stock' : reserved > 0 ? `Add (${reserved} in cart)` : 'Add to cart'}
                </button>
              </footer>
            </article>
          )
        })}
      </div>
    </section>
  )
}
