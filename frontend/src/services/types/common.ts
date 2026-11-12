/**
 * Shapes follow docs/API_CONTRACT.md (v0.3). When the contract changes, this folder changes with it.
 * Operations still in the contract's "Planned" section are served by the mock adapter.
 */

/** ISO 8601 date-time in UTC, e.g. "2026-11-06T10:15:00Z". */
export type IsoDateTime = string

/**
 * Money as a decimal STRING plus ISO 4217 currency. Never a float: the frontend only formats it,
 * and never does financial arithmetic.
 */
export interface Money {
  amount: string
  currency: string
}

/** "demo" records do not come from PayPal and must be labelled as such in the UI. */
export type DataSource = 'paypal' | 'demo'

export type AppLanguage = 'en' | 'de' | 'tr' | 'es'

export interface DateRange {
  from: IsoDateTime
  /** Exclusive upper bound. */
  to: IsoDateTime
}

export interface PageRequest {
  /** 1-based. */
  page: number
  pageSize: number
}

export interface Page<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
}

export type SortDirection = 'asc' | 'desc'

export interface RequestOptions {
  signal?: AbortSignal
  timeoutMs?: number
}
