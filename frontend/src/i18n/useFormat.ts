import { useMemo } from 'react'
import { formatDate, formatDay, formatDateTime, formatMoney, formatNumber, formatPercent } from '@/lib/format'
import { useI18n } from './index'

/** Formatters bound to the active language, so components never pass a locale around. */
export function useFormat() {
  const { intlLocale } = useI18n()

  return useMemo(
    () => ({
      money: (value: Parameters<typeof formatMoney>[0], options?: Parameters<typeof formatMoney>[2]) =>
        formatMoney(value, intlLocale, options),
      number: (value: number, options?: { compact?: boolean }) => formatNumber(value, intlLocale, options),
      percent: (ratio: number, fractionDigits?: number) => formatPercent(ratio, intlLocale, fractionDigits),
      date: (iso: string) => formatDate(iso, intlLocale),
      day: (iso: string, style?: 'short' | 'long') => formatDay(iso, intlLocale, style),
      dateTime: (iso: string) => formatDateTime(iso, intlLocale),
    }),
    [intlLocale],
  )
}
