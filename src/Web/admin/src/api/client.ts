import type { ProblemDetails } from './types'

export const CATALOG_API =
  import.meta.env.VITE_CATALOG_API ?? 'http://localhost:5101'
export const ORDERS_API =
  import.meta.env.VITE_ORDERS_API ?? 'http://localhost:5102'

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

export async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    let message = `Request failed (${response.status})`
    try {
      const problem = (await response.json()) as ProblemDetails
      message = problem.detail ?? problem.title ?? message
    } catch {
      // non-JSON error body; keep default message
    }
    throw new ApiError(message, response.status)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
