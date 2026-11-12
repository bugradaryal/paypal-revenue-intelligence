import { ChartNoAxesCombined, CircleDollarSign } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState } from '@/components/patterns'
import { Button, Card, CardHeader, Skeleton, Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@/components/ui'
import { useRevenueAnalytics } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import { env } from '@/config/env'

export function CustomerCohortsPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const analytics = useRevenueAnalytics(lastDays(90))
  const data = analytics.data
  const maxCohortAge = Math.max(0, ...(data?.cohorts.map((cohort) => cohort.periods.length - 1) ?? []))

  return (
    <>
      <PageHeader title={t('pages.customerCohorts.title')} description={t('pages.customerCohorts.description')} />
      {env.isMock && <p className="mb-5 text-xs text-ink-faint">{t('revenueAnalytics.demoNotice')}</p>}
      <Card>
        <CardHeader title={t('revenueAnalytics.cohorts.title')} description={t('revenueAnalytics.cohorts.description')} />
        {analytics.isPending ? <Skeleton className="h-56 w-full" /> : analytics.isError ? (
          <EmptyState tone="danger" title={errorMessage(analytics.error)} action={
            <Button size="sm" variant="secondary" onClick={() => void analytics.refetch()}>{t('common.retry')}</Button>
          } />
        ) : data && data.cohorts.length > 0 ? (
          <>
            <Table className="min-w-[48rem]">
              <caption className="sr-only">{t('revenueAnalytics.cohorts.title')}</caption>
              <TableHead>
                <tr>
                  <TableHeaderCell>{t('revenueAnalytics.cohorts.month')}</TableHeaderCell>
                  <TableHeaderCell numeric>{t('revenueAnalytics.cohorts.size')}</TableHeaderCell>
                  {Array.from({ length: maxCohortAge + 1 }, (_, age) => (
                    <TableHeaderCell key={age} numeric>{t('revenueAnalytics.cohorts.age', { age })}</TableHeaderCell>
                  ))}
                </tr>
              </TableHead>
              <TableBody>
                {data.cohorts.map((cohort) => (
                  <TableRow key={cohort.cohortMonth}>
                    <TableCell className="whitespace-nowrap font-medium">{f.date(`${cohort.cohortMonth}T00:00:00Z`)}</TableCell>
                    <TableCell numeric>{f.number(cohort.cohortSize)}</TableCell>
                    {Array.from({ length: maxCohortAge + 1 }, (_, age) => {
                      const period = cohort.periods.find((entry) => entry.ageInMonths === age)
                      return (
                        <TableCell key={age} numeric className="min-w-32">
                          {period ? (
                              <div
                                title={t('revenueAnalytics.cohorts.cellHint', {
                                active: f.number(period.activeCustomers),
                                customers: f.number(cohort.cohortSize),
                                revenue: f.money(period.revenuePerCustomer),
                              })}
                                className={`mx-auto grid min-w-24 max-w-32 justify-items-center rounded-lg border px-3 py-2 ${
                                  period.retentionRate >= 0.7 ? 'border-success/25 bg-success/10' :
                                    period.retentionRate >= 0.4 ? 'border-warning/25 bg-warning/10' : 'border-line bg-surface-muted'
                              }`}
                            >
                                <span className="flex items-baseline gap-1.5">
                                  <span className="font-bold text-ink">{f.percent(period.retentionRate, 0)}</span>
                                  <span className="text-[10px] text-ink-faint">
                                    {f.number(period.activeCustomers)}/{f.number(cohort.cohortSize)}
                                  </span>
                                </span>
                                <span className="mt-1.5 h-1.5 w-full overflow-hidden rounded-full bg-surface-muted">
                                  <span
                                    className={`block h-full rounded-full ${
                                      period.retentionRate >= 0.7 ? 'bg-success' :
                                        period.retentionRate >= 0.4 ? 'bg-warning' : 'bg-ink-faint'
                                    }`}
                                    style={{ width: `${Math.max(0, Math.min(100, period.retentionRate * 100))}%` }}
                                  />
                                </span>
                                <span className="mt-1.5 inline-flex items-center gap-1 text-[10px] font-medium text-ink-soft">
                                  <CircleDollarSign className="size-3 text-ink-faint" />
                                  {f.money(period.revenuePerCustomer, { maximumFractionDigits: 0 })}
                                </span>
                              </div>
                          ) : <span className="text-ink-faint">–</span>}
                        </TableCell>
                      )
                    })}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <p className="mt-3 text-xs leading-5 text-ink-faint">{t('revenueAnalytics.cohorts.caveat')}</p>
          </>
        ) : data ? (
          <EmptyState icon={<ChartNoAxesCombined />} title={t('revenueAnalytics.cohorts.empty')} description={t('revenueAnalytics.cohorts.emptyHint')} />
        ) : null}
      </Card>
    </>
  )
}
