import { CATALOG_API, request } from './client'
import type { Category, PagedResult, Product } from './types'

const base = `${CATALOG_API}/api/v1`

// Categories

export function getCategories(): Promise<Category[]> {
  return request(`${base}/categories`)
}

export function createCategory(name: string, description: string): Promise<Category> {
  return request(`${base}/categories`, {
    method: 'POST',
    body: JSON.stringify({ name, description }),
  })
}

export function updateCategory(id: string, name: string, description: string): Promise<Category> {
  return request(`${base}/categories/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name, description }),
  })
}

export function deleteCategory(id: string): Promise<void> {
  return request(`${base}/categories/${id}`, { method: 'DELETE' })
}

// Products

export function searchProducts(params: {
  search?: string
  categoryId?: string
  page?: number
  pageSize?: number
}): Promise<PagedResult<Product>> {
  const query = new URLSearchParams()
  if (params.search) query.set('search', params.search)
  if (params.categoryId) query.set('categoryId', params.categoryId)
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 50))
  return request(`${base}/products?${query}`)
}

export interface CreateProductRequest {
  sku: string
  name: string
  description: string
  price: number
  stockQuantity: number
  categoryId: string
}

export function createProduct(body: CreateProductRequest): Promise<Product> {
  return request(`${base}/products`, { method: 'POST', body: JSON.stringify(body) })
}

export function updateProduct(
  id: string,
  body: { name: string; description: string; categoryId: string },
): Promise<Product> {
  return request(`${base}/products/${id}`, { method: 'PUT', body: JSON.stringify(body) })
}

export function changePrice(id: string, price: number): Promise<Product> {
  return request(`${base}/products/${id}/price`, {
    method: 'PATCH',
    body: JSON.stringify({ price }),
  })
}

export function adjustStock(id: string, delta: number): Promise<Product> {
  return request(`${base}/products/${id}/stock`, {
    method: 'PATCH',
    body: JSON.stringify({ delta }),
  })
}

export function deleteProduct(id: string): Promise<void> {
  return request(`${base}/products/${id}`, { method: 'DELETE' })
}
