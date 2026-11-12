import { useMemo } from 'react'
import { CircleCheck } from 'lucide-react'
import { EmptyState, MoneyText, TrendChart } from '@/components/patterns'
import { Button, Card, CardHeader, Skeleton } from '@/components/ui'
import { env } from '@/config/env'
import { useRevenueAnalytics } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import type { DateRange } from '@/services/types'

export function PaymentFailureAnalysisCard({ range }: { range: DateRange }) {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const analytics = useRevenueAnalytics(range)
  const data = analytics.data

  const failureSeries = useMemo(() => {
    if (!data) return []
    const codes = [...new Set(data.failureTrend.map((point) => point.code))]
    return codes.map((code, index) => ({
      key: code,
      label: tryT(`failureCode.${code}`, t('failureCode.unknown')),
      color: `var(--chart-${(index % 5) + 1})`,
      values: data.daily.map((day) =>
        data.failureTrend
          .filter((point) => point.date === day.date && point.code === code)
          .reduce((total, point) => total + point.count, 0),
      ),
    }))
  }, [data, t, tryT])

  return (
    <Card>
      <CardHeader title={t('revenueAnalytics.failure.title')} description={t('revenueAnalytics.failure.description')} />
      {env.isMock && <p className="mb-4 text-xs text-ink-faint">{t('revenueAnalytics.demoNotice')}</p>}
      <p className="mb-4 text-xs text-ink-faint">{t('revenueAnalytics.failure.scopeNote')}</p>
      {analytics.isPending ? <Skeleton className="h-56 w-full" /> : analytics.isError ? (
        <EmptyState tone="danger" title={errorMessage(analytics.error)} action={
          <Button size="sm" variant="secondary" onClick={() => void analytics.refetch()}>{t('common.retry')}</Button>
        } />
      ) : data ? (
        <>
          {failureSeries.length ? (
            <TrendChart
              ariaLabel={t('revenueAnalytics.failure.chartAria')}
              dates={data.daily.map((day) => `${day.date}T00:00:00Z`)}
              series={failureSeries}
            />
          ) : (
            <EmptyState icon={<CircleCheck />} title={t('revenueAnalytics.failure.noFailures')} />
          )}
          <div className="mt-4 grid gap-2">
            {data.failureReasons.map((reason) => (
              <div key={reason.code} className="flex items-center justify-between gap-3 border-t border-line pt-3 text-sm">
                <div className="min-w-0">
                  <p className="truncate font-medium">{tryT(`failureCode.${reason.code}`, t('failureCode.unknown'))}</p>
                  <p className="text-xs text-ink-faint">{t('revenueAnalytics.failure.reasonCount', { count: f.number(reason.count) })}</p>
                </div>
                <MoneyText value={reason.amountUsd} className="font-semibold" />
              </div>
            ))}
            <p className="text-xs text-ink-faint">{t('revenueAnalytics.failure.amountNote')}</p>
          </div>
        </>
      ) : null}
    </Card>
  )
}
