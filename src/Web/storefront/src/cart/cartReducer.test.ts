import { describe, expect, it } from 'vitest'
import type { Product } from '../api/types'
import { cartCount, cartReducer, cartTotal, emptyCart, type CartState } from './cartReducer'

const product = (overrides: Partial<Product> = {}): Product => ({
  id: 'p1',
  sku: 'SKU-1',
  name: 'Widget',
  description: 'A widget',
  price: 10,
  stockQuantity: 5,
  categoryId: 'c1',
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: null,
  ...overrides,
})

describe('cartReducer', () => {
  it('adds a new product as a line', () => {
    const state = cartReducer(emptyCart, { type: 'add', product: product() })

    expect(state.lines).toHaveLength(1)
    expect(state.lines[0]).toMatchObject({ productId: 'p1', quantity: 1, unitPrice: 10 })
  })

  it('increments quantity when adding the same product twice', () => {
    let state = cartReducer(emptyCart, { type: 'add', product: product() })
    state = cartReducer(state, { type: 'add', product: product() })

    expect(state.lines).toHaveLength(1)
    expect(state.lines[0].quantity).toBe(2)
  })

  it('caps quantity at available stock', () => {
    let state = cartReducer(emptyCart, { type: 'add', product: product({ stockQuantity: 3 }) })
    state = cartReducer(state, { type: 'setQuantity', productId: 'p1', quantity: 99 })

    expect(state.lines[0].quantity).toBe(3)
  })

  it('ignores adding an out-of-stock product', () => {
    const state = cartReducer(emptyCart, { type: 'add', product: product({ stockQuantity: 0 }) })

    expect(state.lines).toHaveLength(0)
  })

  it('removes a line when quantity is set below 1', () => {
    let state = cartReducer(emptyCart, { type: 'add', product: product() })
    state = cartReducer(state, { type: 'setQuantity', productId: 'p1', quantity: 0 })

    expect(state.lines).toHaveLength(0)
  })

  it('computes totals and counts across lines', () => {
    let state: CartState = emptyCart
    state = cartReducer(state, { type: 'add', product: product(), quantity: 2 })
    state = cartReducer(state, {
      type: 'add',
      product: product({ id: 'p2', sku: 'SKU-2', price: 2.5 }),
    })

    expect(cartTotal(state)).toBe(22.5)
    expect(cartCount(state)).toBe(3)
  })

  it('clears the cart', () => {
    let state = cartReducer(emptyCart, { type: 'add', product: product() })
    state = cartReducer(state, { type: 'clear' })

    expect(state).toEqual(emptyCart)
  })
})
