import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'

export function LogoMark({ className }: { className?: string }) {
  return (
    <img src="/app.png" alt="" aria-hidden="true" className={cn('size-9 shrink-0 object-contain', className)} />
  )
}

export function Logo({ className }: { className?: string }) {
  const { t } = useI18n()
  return (
    <div className={cn('flex items-center', className)}>
      <img
        src="/navbar.png"
        alt={`${t('app.name')} ${t('app.vendor')}`}
        className="h-14 w-auto max-w-full object-contain"
      />
    </div>
  )
}
