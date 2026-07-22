import {
  createContext,
  useContext,
  useEffect,
  useReducer,
  type Dispatch,
  type ReactNode,
} from 'react'
import { cartReducer, emptyCart, type CartAction, type CartState } from './cartReducer'

const STORAGE_KEY = 'nexuscommerce.cart'

interface CartContextValue {
  cart: CartState
  dispatch: Dispatch<CartAction>
}

const CartContext = createContext<CartContextValue | null>(null)

function loadCart(): CartState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return JSON.parse(raw) as CartState
  } catch {
    // corrupted storage — start fresh
  }
  return emptyCart
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [cart, dispatch] = useReducer(cartReducer, undefined, loadCart)

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(cart))
  }, [cart])

  return <CartContext.Provider value={{ cart, dispatch }}>{children}</CartContext.Provider>
}

export function useCart(): CartContextValue {
  const value = useContext(CartContext)
  if (!value) throw new Error('useCart must be used inside <CartProvider>')
  return value
}
