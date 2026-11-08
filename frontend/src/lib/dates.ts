import type { DateRange } from '@/services/types'

export const DAY_MS = 86_400_000

export function startOfUtcDay(ms: number): number {
  return Math.floor(ms / DAY_MS) * DAY_MS
}

/**
 * The last `days` whole UTC days, including today. Boundaries are rounded to midnight, so the value
 * is stable for a whole day and safe to use inside query keys.
 */
export function lastDays(days: number, now: number = Date.now()): DateRange {
  const to = startOfUtcDay(now) + DAY_MS
  return { from: new Date(to - days * DAY_MS).toISOString(), to: new Date(to).toISOString() }
}
