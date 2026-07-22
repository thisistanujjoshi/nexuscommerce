import { ORDERS_API, request } from './client'
import type { Order, OrderStatus, PagedResult } from './types'

const base = `${ORDERS_API}/api/v1/orders`

export function getOrders(params: {
  status?: OrderStatus
  page?: number
  pageSize?: number
}): Promise<PagedResult<Order>> {
  const query = new URLSearchParams()
  if (params.status) query.set('status', params.status)
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 50))
  return request(`${base}?${query}`)
}

export type OrderTransition = 'confirm' | 'ship' | 'deliver' | 'cancel'

export function transitionOrder(id: string, transition: OrderTransition): Promise<Order> {
  return request(`${base}/${id}/${transition}`, { method: 'POST' })
}
