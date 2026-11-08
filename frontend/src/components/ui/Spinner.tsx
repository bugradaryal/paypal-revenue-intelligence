import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'

export function Spinner({ className, label }: { className?: string; label?: string }) {
  const { t } = useI18n()

  return (
    <svg
      role="status"
      aria-label={label ?? t('common.loading')}
      viewBox="0 0 24 24"
      className={cn('size-4 animate-spin', className)}
      fill="none"
    >
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeOpacity="0.25" strokeWidth="3" />
      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  )
}
