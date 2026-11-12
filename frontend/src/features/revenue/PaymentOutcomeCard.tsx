import { useMemo } from 'react'
import { CircleCheck } from 'lucide-react'
import { EmptyState, MoneyText } from '@/components/patterns'
import { Button, Card, CardHeader, Skeleton } from '@/components/ui'
import { env } from '@/config/env'
import { useRevenueAnalytics } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import type { DateRange, Money } from '@/services/types'

interface WaterfallStep {
  key: string
  label: string
  amount: Money
  start: number
  end: number
  tone: 'brand' | 'danger' | 'warning' | 'neutral' | 'success'
}

export function PaymentOutcomeCard({ range }: { range: DateRange }) {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const analytics = useRevenueAnalytics(range)
  const data = analytics.data

  const waterfall = useMemo<WaterfallStep[]>(() => {
    if (!data) return []
    const total = Number(data.leakage.transactionActivity.amount)
    if (!(total > 0)) return []

    let remaining = total
    const subtract = (key: string, label: string, amount: Money, tone: WaterfallStep['tone']) => {
      const value = Number(amount.amount)
      const start = Math.max(0, remaining - value)
      const step = { key, label, amount, start, end: remaining, tone }
      remaining = start
      return step
    }

    return [
      { key: 'activity', label: t('revenueAnalytics.waterfall.activity'), amount: data.leakage.transactionActivity, start: 0, end: total, tone: 'brand' },
      subtract('failed', t('revenueAnalytics.waterfall.failed'), data.leakage.failedAttemptAmount, 'danger'),
      subtract('pending', t('revenueAnalytics.waterfall.pending'), data.leakage.pendingAmount, 'warning'),
      subtract('refunded', t('revenueAnalytics.waterfall.refunded'), data.leakage.refundedAmount, 'neutral'),
      { key: 'completed', label: t('revenueAnalytics.waterfall.completed'), amount: data.leakage.completedAmount, start: 0, end: Number(data.leakage.completedAmount.amount), tone: 'success' },
    ]
  }, [data, t])

  return (
    <Card>
      <CardHeader title={t('revenueAnalytics.waterfall.title')} description={t('revenueAnalytics.waterfall.description')} />
      {env.isMock && <p className="mb-4 text-xs text-ink-faint">{t('revenueAnalytics.demoNotice')}</p>}
      {analytics.isPending ? (
        <Skeleton className="h-56 w-full" />
      ) : analytics.isError ? (
        <EmptyState tone="danger" title={errorMessage(analytics.error)} action={
          <Button size="sm" variant="secondary" onClick={() => void analytics.refetch()}>{t('common.retry')}</Button>
        } />
      ) : data ? (
        <>
          {waterfall.length ? (
            <div className="grid gap-4" role="group" aria-label={t('revenueAnalytics.waterfall.aria')}>
              <div className="ml-[min(42%,12rem)] h-2 rounded-full bg-surface-muted" aria-hidden="true">
                <div className="h-full rounded-full bg-brand-500" style={{ width: '100%' }} />
              </div>
              <div className="grid gap-3">
                {waterfall.map((step) => {
                  const total = Number(data.leakage.transactionActivity.amount)
                  const left = total > 0 ? (step.start / total) * 100 : 0
                  const width = total > 0 ? ((step.end - step.start) / total) * 100 : 0
                  const color = step.tone === 'danger' ? 'bg-danger' :
                    step.tone === 'warning' ? 'bg-warning' :
                      step.tone === 'success' ? 'bg-success' :
                        step.tone === 'neutral' ? 'bg-ink-faint' : 'bg-brand-500'
                  return (
                    <div key={step.key} className="grid grid-cols-[minmax(7rem,12rem)_minmax(0,1fr)_auto] items-center gap-3 text-sm">
                      <span className="text-ink-soft">{step.label}</span>
                      <div className="relative h-7 rounded bg-surface-muted/70">
                        <span className={`absolute top-0 h-full min-w-1 rounded ${color}`} style={{ left: `${left}%`, width: `${width}%` }} />
                      </div>
                      <MoneyText value={step.amount} className="font-semibold" />
                    </div>
                  )
                })}
              </div>
            </div>
          ) : (
            <EmptyState icon={<CircleCheck />} title={t('revenueAnalytics.waterfall.empty')} />
          )}
          <div className="mt-5 grid gap-2 rounded-xl border border-line bg-surface-muted/50 p-4 text-xs leading-5 text-ink-soft sm:grid-cols-2">
            <p>{t('revenueAnalytics.waterfall.invoiceSnapshot', {
              open: f.money(data.leakage.currentOpenReceivables),
              overdue: f.money(data.leakage.currentOverdueReceivables),
            })}</p>
            <p>{t('revenueAnalytics.waterfall.caveat')}</p>
            {!data.leakage.cancellationTrackingAvailable && <p className="sm:col-span-2">{t('revenueAnalytics.waterfall.cancellationsUnavailable')}</p>}
            {data.excludedPaymentCount > 0 && <p className="sm:col-span-2">{t('revenueAnalytics.excluded', { count: f.number(data.excludedPaymentCount) })}</p>}
          </div>
        </>
      ) : null}
    </Card>
  )
}
