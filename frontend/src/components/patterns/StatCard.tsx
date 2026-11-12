import type { ReactNode } from 'react'
import { RefreshCw } from 'lucide-react'
import { Button, Card, Skeleton } from '@/components/ui'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'

interface StatCardProps {
  label: string
  /** The formatted number. Format it with MoneyText / useFormat; the card never calculates. */
  value?: ReactNode
  hint?: ReactNode
  icon?: ReactNode
  /** Accent for the icon chip only; the number stays in text colour. */
  tone?: 'brand' | 'success' | 'warning' | 'danger' | 'neutral'
  loading?: boolean
  /** Already translated message. Shows an error state with a retry button. */
  error?: string
  onRetry?: () => void
  className?: string
}

const TONE: Record<NonNullable<StatCardProps['tone']>, string> = {
  brand: 'bg-brand-soft text-brand-text',
  success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
  neutral: 'bg-surface-muted text-ink-soft',
}

const GLOW: Record<NonNullable<StatCardProps['tone']>, string> = {
  brand: 'bg-brand-400/20',
  success: 'bg-success/10',
  warning: 'bg-warning/10',
  danger: 'bg-danger/10',
  neutral: 'bg-surface-muted',
}

export function StatCard({ label, value, hint, icon, tone = 'neutral', loading, error, onRetry, className }: StatCardProps) {
  const { t } = useI18n()

  return (
    <Card padding="sm" className={cn('group relative grid min-w-0 content-between gap-5 overflow-hidden p-5 transition-[transform,box-shadow,border-color] duration-300 hover:-translate-y-0.5 hover:border-brand-300/70 hover:shadow-md', className)}>
      <span aria-hidden="true" className={cn('pointer-events-none absolute -right-8 -top-10 size-32 rounded-full blur-2xl transition-opacity duration-300 group-hover:opacity-90', GLOW[tone])} />
      <div className="relative flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-ink-soft">{label}</p>
        {icon && <span className={cn('grid size-10 shrink-0 place-items-center rounded-xl shadow-xs [&_svg]:size-[1.1rem]', TONE[tone])}>{icon}</span>}
      </div>

      {loading ? (
        <div className="relative grid gap-2">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-3 w-24" />
        </div>
      ) : error ? (
        <div className="relative grid justify-items-start gap-2">
          <p role="alert" className="text-sm text-danger">
            {error}
          </p>
          {onRetry && (
            <Button size="sm" variant="ghost" leadingIcon={<RefreshCw className="size-4" />} onClick={onRetry}>
              {t('common.retry')}
            </Button>
          )}
        </div>
      ) : (
        <div className="relative grid gap-1">
          <p className="min-w-0 truncate text-2xl font-extrabold tracking-[-0.04em] sm:text-[1.8rem]">{value}</p>
          {hint && <p className="text-xs text-ink-soft">{hint}</p>}
        </div>
      )}
    </Card>
  )
}
