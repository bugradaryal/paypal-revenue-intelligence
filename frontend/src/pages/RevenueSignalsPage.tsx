import { useMemo, type ReactNode } from 'react'
import { AlertTriangle, ArrowUpRight, BellRing, CircleCheck, Coins, FileClock, Sparkles } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState, MoneyText, StatCard } from '@/components/patterns'
import { Badge, Button, Card, Skeleton } from '@/components/ui'
import { env } from '@/config/env'
import { useCashFlowOutlook, useIssues, useRecoverySummary, useRevenueAnalytics, useSuggestions } from '@/features/revenue/queries'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { useI18n } from '@/i18n'
import { lastDays } from '@/lib/dates'
import type { Money } from '@/services/types'

type SignalTone = 'danger' | 'warning' | 'brand' | 'info'

interface Signal {
  id: string
  title: string
  description: string
  href: string
  tone: SignalTone
  icon: ReactNode
  count?: number
  amount?: Money
}

export function RevenueSignalsPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const range = useMemo(() => lastDays(30), [])
  const cashFlow = useCashFlowOutlook({ refetchInterval: 60_000 })
  const openIssues = useIssues({ page: 1, pageSize: 1, status: ['open'] }, { refetchInterval: 60_000 })
  const failedActions = useSuggestions({ page: 1, pageSize: 1, status: ['failed'] }, { refetchInterval: 60_000 })
  const recovery = useRecoverySummary(range, { refetchInterval: 60_000 })
  const analytics = useRevenueAnalytics(range, { refetchInterval: 300_000 })
  const loading = cashFlow.isPending || openIssues.isPending || failedActions.isPending || recovery.isPending
  const failed = [cashFlow, openIssues, failedActions, recovery].find((query) => query.isError)
  const signals = useMemo<Signal[]>(() => {
    const items: Signal[] = []
    const cash = cashFlow.data
    if (cash?.overdueInvoiceCount) {
      items.push({
        id: 'overdue',
        title: t('signals.alert.overdue.title', { count: f.number(cash.overdueInvoiceCount) }),
        description: t('signals.alert.overdue.description'),
        href: '/cash-flow#receivables',
        tone: 'danger',
        icon: <AlertTriangle className="size-5" />,
        count: cash.overdueInvoiceCount,
        amount: cash.overdueReceivables,
      })
    }
    if (cash?.dueWithin7DaysCount) {
      items.push({
        id: 'due-soon',
        title: t('signals.alert.dueSoon.title', { count: f.number(cash.dueWithin7DaysCount) }),
        description: t('signals.alert.dueSoon.description'),
        href: '/cash-flow#receivables',
        tone: 'warning',
        icon: <FileClock className="size-5" />,
        count: cash.dueWithin7DaysCount,
        amount: cash.dueWithin7Days,
      })
    }
    if (recovery.data?.openSuggestionCount) {
      items.push({
        id: 'approval',
        title: t('signals.alert.approvals.title', { count: f.number(recovery.data.openSuggestionCount) }),
        description: t('signals.alert.approvals.description'),
        href: '/command-center',
        tone: 'brand',
        icon: <Sparkles className="size-5" />,
        count: recovery.data.openSuggestionCount,
      })
    }
    if (openIssues.data?.totalItems) {
      items.push({
        id: 'open-issues',
        title: t('signals.alert.openIssues.title', { count: f.number(openIssues.data.totalItems) }),
        description: t('signals.alert.openIssues.description'),
        href: '/command-center',
        tone: 'warning',
        icon: <BellRing className="size-5" />,
        count: openIssues.data.totalItems,
      })
    }
    if (failedActions.data?.totalItems) {
      items.push({
        id: 'failed-actions',
        title: t('signals.alert.failedActions.title', { count: f.number(failedActions.data.totalItems) }),
        description: t('signals.alert.failedActions.description'),
        href: '/command-center',
        tone: 'danger',
        icon: <AlertTriangle className="size-5" />,
        count: failedActions.data.totalItems,
      })
    }
    if (cash?.foreignCurrencyExposure.length) {
      items.push({
        id: 'currency',
        title: t('signals.alert.currency.title', { count: f.number(cash.foreignCurrencyExposure.length) }),
        description: t('signals.alert.currency.description'),
        href: '/cash-flow#currency-exposure',
        tone: 'info',
        icon: <Coins className="size-5" />,
        count: cash.foreignCurrencyExposure.length,
      })
    }
    return items
  }, [cashFlow.data, failedActions.data, f, openIssues.data, recovery.data, t])

  return (
    <>
      <PageHeader title={t('pages.signals.title')} description={t('pages.signals.description')} />

      {env.isMock && <p className="mb-5 text-xs text-ink-faint">{t('signals.demoNotice')}</p>}

      <section aria-label={t('signals.summaryLabel')} className="mb-6 grid gap-4 sm:grid-cols-3">
        <StatCard label={t('signals.stats.active')} icon={<BellRing />} tone={signals.length ? 'warning' : 'success'}
          loading={loading} error={failed ? errorMessage(failed.error) : undefined}
          value={<span className="tabular">{f.number(signals.length)}</span>} hint={t('signals.stats.activeHint')} />
        <StatCard label={t('signals.stats.openIssues')} icon={<AlertTriangle />} tone="danger" loading={openIssues.isPending}
          error={openIssues.isError ? errorMessage(openIssues.error) : undefined} onRetry={() => void openIssues.refetch()}
          value={openIssues.data && <span className="tabular">{f.number(openIssues.data.totalItems)}</span>}
          hint={t('signals.stats.openIssuesHint')} />
        <StatCard label={t('signals.stats.failedActions')} icon={<ArrowUpRight />} tone="danger" loading={failedActions.isPending}
          error={failedActions.isError ? errorMessage(failedActions.error) : undefined} onRetry={() => void failedActions.refetch()}
          value={failedActions.data && <span className="tabular">{f.number(failedActions.data.totalItems)}</span>}
          hint={t('signals.stats.failedActionsHint')} />
      </section>

      <Card className="grid gap-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">{t('signals.list.title')}</h2>
            <p className="mt-1 text-sm text-ink-soft">{t('signals.list.description')}</p>
          </div>
          <Badge dot tone="info">{t('signals.list.liveBadge')}</Badge>
        </div>

        {loading ? (
          <div className="grid gap-3">{[0, 1, 2].map((item) => <Skeleton key={item} className="h-28 w-full" />)}</div>
        ) : failed ? (
          <EmptyState tone="danger" title={errorMessage(failed.error)} action={
            <Button size="sm" variant="secondary" onClick={() => {
              void cashFlow.refetch()
              void openIssues.refetch()
              void failedActions.refetch()
              void recovery.refetch()
            }}>{t('common.retry')}</Button>
          } />
        ) : signals.length ? (
          <div className="grid gap-3">
            {signals.map((signal) => (
              <Link
                key={signal.id}
                to={signal.href}
                className={`focus-ring flex flex-wrap items-center justify-between gap-4 rounded-xl border p-4 transition-colors hover:border-brand-300 ${
                  signal.tone === 'danger' ? 'border-danger/25 bg-danger-soft' :
                    signal.tone === 'warning' ? 'border-warning/25 bg-warning-soft' :
                      signal.tone === 'brand' ? 'border-brand-200 bg-brand-50' : 'border-line bg-surface-muted'
                }`}
              >
                <div className="flex min-w-0 items-start gap-3">
                  <span className="mt-0.5 text-ink-soft">{signal.icon}</span>
                  <div className="min-w-0">
                    <p className="font-semibold">{signal.title}</p>
                    <p className="mt-1 text-sm text-ink-soft">{signal.description}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  {signal.amount && <MoneyText value={signal.amount} className="font-semibold" />}
                  {signal.count !== undefined && !signal.amount && <Badge tone={signal.tone}>{f.number(signal.count)}</Badge>}
                  <ArrowUpRight className="size-4 shrink-0 text-ink-faint" />
                </div>
              </Link>
            ))}
          </div>
        ) : (
          <EmptyState icon={<CircleCheck />} title={t('signals.list.empty')} description={t('signals.list.emptyHint')} />
        )}
      </Card>

      <Card className="mt-6 grid gap-4">
        <div>
          <h2 className="text-lg font-semibold">{t('revenueAnalytics.anomalies.title')}</h2>
          <p className="mt-1 text-sm text-ink-soft">
            {t('revenueAnalytics.anomalies.description', { days: analytics.data?.anomalies.windowDays ?? 14 })}
          </p>
        </div>
        {analytics.isPending ? (
          <Skeleton className="h-28 w-full" />
        ) : analytics.isError ? (
          <EmptyState tone="danger" title={errorMessage(analytics.error)} action={
            <Button size="sm" variant="secondary" onClick={() => void analytics.refetch()}>{t('common.retry')}</Button>
          } />
        ) : analytics.data ? (
          <div className="grid gap-3">
            {analytics.data.anomalies.failureRateSpike && (
              <div className="rounded-xl border border-danger/25 bg-danger-soft p-4">
                <p className="font-semibold">{t('revenueAnalytics.anomalies.failureSpike')}</p>
                <p className="mt-1 text-sm text-ink-soft">
                  {t('revenueAnalytics.anomalies.rateComparison', {
                    current: f.percent(analytics.data.anomalies.failureRateSpike.currentRate),
                    baseline: f.percent(analytics.data.anomalies.failureRateSpike.baselineRate),
                    currentCount: f.number(analytics.data.anomalies.failureRateSpike.currentFailedCount),
                    baselineCount: f.number(analytics.data.anomalies.failureRateSpike.baselinePaymentCount),
                  })}
                </p>
                <Link to="/payments?range=d30&status=failed" className="mt-2 inline-flex items-center gap-1 text-sm font-semibold underline">
                  {t('pages.payments.title')} <ArrowUpRight className="size-4" />
                </Link>
              </div>
            )}
            {analytics.data.anomalies.revenueDrops.map((drop) => (
              <div key={drop.currency} className="rounded-xl border border-warning/25 bg-warning-soft p-4">
                <p className="font-semibold">{t('revenueAnalytics.anomalies.revenueDrop', { currency: drop.currency })}</p>
                <p className="mt-1 text-sm text-ink-soft">
                  {t('revenueAnalytics.anomalies.amountComparison', {
                    current: f.money(drop.currentRevenue),
                    baseline: f.money(drop.baselineRevenue),
                    change: f.number(Math.round(Math.abs(drop.changePercent) * 10) / 10),
                  })}
                </p>
                <Link to="/payments?range=d30" className="mt-2 inline-flex items-center gap-1 text-sm font-semibold underline">
                  {t('pages.payments.title')} <ArrowUpRight className="size-4" />
                </Link>
              </div>
            ))}
            {!analytics.data.anomalies.failureRateSpike && analytics.data.anomalies.revenueDrops.length === 0 && (
              <EmptyState icon={<CircleCheck />} title={t('revenueAnalytics.anomalies.noSignals')} description={t('revenueAnalytics.anomalies.noSignalsHint')} />
            )}
            <p className="text-xs leading-5 text-ink-faint">{t('revenueAnalytics.anomalies.thresholds')}</p>
          </div>
        ) : null}
      </Card>
    </>
  )
}
