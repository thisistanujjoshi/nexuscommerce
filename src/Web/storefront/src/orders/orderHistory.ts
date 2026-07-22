const STORAGE_KEY = 'nexuscommerce.orderIds'

/** Order ids placed from this browser, newest first (demo stand-in for an account). */
export function knownOrderIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return JSON.parse(raw) as string[]
  } catch {
    // corrupted storage — start fresh
  }
  return []
}

export function rememberOrder(id: string): void {
  const ids = [id, ...knownOrderIds().filter((existing) => existing !== id)]
  localStorage.setItem(STORAGE_KEY, JSON.stringify(ids.slice(0, 50)))
}
