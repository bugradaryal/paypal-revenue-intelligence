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

export function StatCard({ label, value, hint, icon, tone = 'neutral', loading, error, onRetry, className }: StatCardProps) {
  const { t } = useI18n()

  return (
    <Card padding="sm" className={cn('grid min-w-0 content-between gap-4 p-5', className)}>
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-ink-soft">{label}</p>
        {icon && <span className={cn('grid size-8 shrink-0 place-items-center rounded-lg [&_svg]:size-4', TONE[tone])}>{icon}</span>}
      </div>

      {loading ? (
        <div className="grid gap-2">
          <Skeleton className="h-8 w-32" />
          <Skeleton className="h-3 w-24" />
        </div>
      ) : error ? (
        <div className="grid justify-items-start gap-2">
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
        <div className="grid gap-1">
          <p className="min-w-0 truncate text-2xl font-bold tracking-tight sm:text-[1.7rem]">{value}</p>
          {hint && <p className="text-xs text-ink-soft">{hint}</p>}
        </div>
      )}
    </Card>
  )
}
