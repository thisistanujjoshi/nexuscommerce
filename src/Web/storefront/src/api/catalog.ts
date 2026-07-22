import { CATALOG_API, request } from './client'
import type { Category, PagedResult, Product } from './types'

export function getCategories(): Promise<Category[]> {
  return request(`${CATALOG_API}/api/v1/categories`)
}

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
  query.set('pageSize', String(params.pageSize ?? 24))
  return request(`${CATALOG_API}/api/v1/products?${query}`)
}
