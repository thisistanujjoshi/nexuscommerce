export interface Category {
  id: string
  name: string
  description: string
}

export interface Product {
  id: string
  sku: string
  name: string
  description: string
  price: number
  stockQuantity: number
  categoryId: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type OrderStatus = 'Pending' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled'

export interface OrderItem {
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface Order {
  id: string
  customerId: string
  customerEmail: string
  status: OrderStatus
  total: number
  placedAtUtc: string
  updatedAtUtc: string | null
  items: OrderItem[]
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}
