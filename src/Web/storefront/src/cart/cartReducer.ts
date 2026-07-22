import type { Product } from '../api/types'

export interface CartLine {
  productId: string
  name: string
  sku: string
  unitPrice: number
  quantity: number
  /** Stock known at add time — used to cap quantity client-side. */
  availableStock: number
}

export interface CartState {
  lines: CartLine[]
}

export type CartAction =
  | { type: 'add'; product: Product; quantity?: number }
  | { type: 'setQuantity'; productId: string; quantity: number }
  | { type: 'remove'; productId: string }
  | { type: 'clear' }

export const emptyCart: CartState = { lines: [] }

export function cartTotal(state: CartState): number {
  return state.lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0)
}

export function cartCount(state: CartState): number {
  return state.lines.reduce((sum, line) => sum + line.quantity, 0)
}

export function cartReducer(state: CartState, action: CartAction): CartState {
  switch (action.type) {
    case 'add': {
      const quantity = action.quantity ?? 1
      const existing = state.lines.find((l) => l.productId === action.product.id)
      if (existing) {
        return cartReducer(state, {
          type: 'setQuantity',
          productId: existing.productId,
          quantity: existing.quantity + quantity,
        })
      }
      if (action.product.stockQuantity < 1) return state
      return {
        lines: [
          ...state.lines,
          {
            productId: action.product.id,
            name: action.product.name,
            sku: action.product.sku,
            unitPrice: action.product.price,
            quantity: Math.min(quantity, action.product.stockQuantity),
            availableStock: action.product.stockQuantity,
          },
        ],
      }
    }
    case 'setQuantity': {
      if (action.quantity < 1) {
        return cartReducer(state, { type: 'remove', productId: action.productId })
      }
      return {
        lines: state.lines.map((line) =>
          line.productId === action.productId
            ? { ...line, quantity: Math.min(action.quantity, line.availableStock) }
            : line,
        ),
      }
    }
    case 'remove':
      return { lines: state.lines.filter((l) => l.productId !== action.productId) }
    case 'clear':
      return emptyCart
  }
}
