import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'

export function LogoMark({ className }: { className?: string }) {
  return (
    <span
      aria-hidden
      className={cn('grid size-9 shrink-0 place-items-center rounded-md bg-brand-gradient shadow-brand', className)}
    >
      <svg viewBox="0 0 32 32" className="size-5" fill="none">
        <path
          d="M9 22V10h7a4 4 0 0 1 0 8H9M15 18l6 4"
          stroke="var(--ink-on-brand)"
          strokeWidth="2.8"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </span>
  )
}

export function Logo({ className }: { className?: string }) {
  const { t } = useI18n()
  return (
    <div className={cn('flex items-center gap-3', className)}>
      <LogoMark />
      <div className="leading-tight">
        <p className="text-sm font-bold tracking-tight">{t('app.name')}</p>
        <p className="text-[11px] font-medium text-ink-faint">{t('app.vendor')}</p>
      </div>
    </div>
  )
}
