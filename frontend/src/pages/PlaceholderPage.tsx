import { PageHeader } from '@/components/layout/PageHeader'
import { findNavItemByPage, type PageKey } from '@/components/layout/navigation'
import { Card } from '@/components/ui'
import { useI18n } from '@/i18n'

/**
 * Temporary body for screens that are built in later phases.
 * It deliberately shows no numbers: sample data must never look like real data.
 */
export function PlaceholderPage({ page }: { page: PageKey }) {
  const { t } = useI18n()
  const Icon = findNavItemByPage(page)?.icon

  return (
    <>
      <PageHeader title={t(`pages.${page}.title`)} description={t(`pages.${page}.description`)} />
      <Card className="grid place-items-center gap-3 py-16 text-center">
        {Icon && (
          <span className="grid size-14 place-items-center rounded-xl bg-brand-soft text-brand-500">
            <Icon className="size-6" />
          </span>
        )}
        <p className="text-base font-semibold">{t('common.comingSoon')}</p>
        <p className="max-w-sm text-sm text-ink-soft">{t('common.noDataNote')}</p>
      </Card>
    </>
  )
}
