import { ORDERS_API, request } from './client'
import type { Order, PlaceOrderItem } from './types'

export function placeOrder(
  customerId: string,
  customerEmail: string,
  items: PlaceOrderItem[],
): Promise<Order> {
  return request(`${ORDERS_API}/api/v1/orders`, {
    method: 'POST',
    body: JSON.stringify({ customerId, customerEmail, items }),
  })
}

export function getOrder(id: string): Promise<Order> {
  return request(`${ORDERS_API}/api/v1/orders/${id}`)
}
