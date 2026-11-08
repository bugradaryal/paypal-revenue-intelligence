import type { IsoDateTime, Money } from '@/services/types'

/**
 * Display-only formatting. Money arrives as a decimal string; converting to a number here is fine
 * because the result is only shown, never used for calculations.
 */

export interface MoneyFormatOptions {
  /** 1.2K / 3.4M style. Good for stat cards. */
  compact?: boolean
  /** Use for tiny amounts such as per-request AI cost. */
  maximumFractionDigits?: number
}

export function formatMoney(money: Money, locale: string, options: MoneyFormatOptions = {}): string {
  const value = Number(money.amount)
  if (!Number.isFinite(value)) return `${money.amount} ${money.currency}`

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: money.currency,
    notation: options.compact ? 'compact' : 'standard',
    maximumFractionDigits: options.maximumFractionDigits,
  }).format(value)
}

export function formatNumber(value: number, locale: string, options: { compact?: boolean } = {}): string {
  return new Intl.NumberFormat(locale, { notation: options.compact ? 'compact' : 'standard' }).format(value)
}

/** `ratio` is 0..1. */
export function formatPercent(ratio: number, locale: string, fractionDigits = 1): string {
  return new Intl.NumberFormat(locale, {
    style: 'percent',
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(ratio)
}

export function formatDate(iso: IsoDateTime, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(iso))
}

/** A UTC calendar day (chart buckets are UTC days), shown without shifting it into the viewer's time zone. */
export function formatDay(iso: IsoDateTime, locale: string, style: 'short' | 'long' = 'short'): string {
  return new Intl.DateTimeFormat(
    locale,
    style === 'short' ? { month: 'short', day: 'numeric', timeZone: 'UTC' } : { dateStyle: 'medium', timeZone: 'UTC' },
  ).format(new Date(iso))
}

export function formatDateTime(iso: IsoDateTime, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso))
}
