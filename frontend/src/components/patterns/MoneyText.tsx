import { useFormat } from '@/i18n/useFormat'
import { cn } from '@/lib/cn'
import type { Money } from '@/services/types'

interface MoneyTextProps {
  value: Money
  /** 1.2K style. The exact amount stays available as a tooltip. */
  compact?: boolean
  maximumFractionDigits?: number
  className?: string
}

/** The one way money is displayed. Formats only; never calculates. */
export function MoneyText({ value, compact, maximumFractionDigits, className }: MoneyTextProps) {
  const f = useFormat()
  const exact = f.money(value, { maximumFractionDigits })

  return (
    <span className={cn('tabular whitespace-nowrap', className)} title={compact ? exact : undefined}>
      {compact ? f.money(value, { compact, maximumFractionDigits }) : exact}
    </span>
  )
}
