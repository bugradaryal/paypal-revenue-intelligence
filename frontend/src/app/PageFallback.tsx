import { Skeleton } from '@/components/ui'
import { useI18n } from '@/i18n'

export function PageFallback() {
  const { t } = useI18n()
  return (
    <div role="status" aria-label={t('common.loading')} className="grid gap-4">
      <Skeleton className="h-9 w-56" />
      <Skeleton className="h-4 w-80 max-w-full" />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Skeleton className="h-28" />
        <Skeleton className="h-28" />
        <Skeleton className="h-28" />
        <Skeleton className="h-28" />
      </div>
      <Skeleton className="h-64" />
    </div>
  )
}
