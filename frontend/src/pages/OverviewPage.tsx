import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { ArrowRight, Clock, CircleAlert, Sparkles, Undo2, Wallet } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Badge, Button, Card, CardHeader, Skeleton, buttonStyles } from '@/components/ui'
import {
  DataTable,
  EmptyState,
  MoneyText,
  SegmentedControl,
  StatCard,
  StatusPill,
  TrendChart,
  type Column,
} from '@/components/patterns'
import { useAiUsageSummary } from '@/features/ai-cost/queries'
import { usePayments, useRecoverySummary, useRevenueSummary } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import type { Payment } from '@/services/types'

const RANGES = { d7: 7, d30: 30, d90: 90 } as const
type RangeKey = keyof typeof RANGES

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-line py-2.5 text-sm last:border-0">
      <dt className="text-ink-soft">{label}</dt>
      <dd className="min-w-0 text-right font-semibold">{children}</dd>
    </div>
  )
}

function Rows({ loading }: { loading: boolean }) {
  return loading ? (
    <div className="grid gap-3">
      {[0, 1, 2, 3].map((i) => (
        <Skeleton key={i} className="h-5 w-full" />
      ))}
    </div>
  ) : null
}

export function OverviewPage() {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const [rangeKey, setRangeKey] = useState<RangeKey>('d30')
  const range = useMemo(() => lastDays(RANGES[rangeKey]), [rangeKey])

  const summary = useRevenueSummary(range)
  const recovery = useRecoverySummary(range)
  const failed = usePayments({ page: 1, pageSize: 5, status: ['failed'], range, sortBy: 'createdAt', sortDir: 'desc' })
  const ai = useAiUsageSummary(range)

  const s = summary.isError ? undefined : summary.data
  const summaryError = summary.isError ? errorMessage(summary.error) : undefined

  const columns: Column<Payment>[] = [
    {
      key: 'customer',
      header: t('common.customer'),
      cell: (p) => (
        <div className="min-w-0">
          <p className="flex items-center gap-2 font-medium">
            <span className="truncate">{p.customerName}</span>
            {p.dataSource === 'demo' && (
              <Badge size="sm" tone="neutral">
                {t('common.demo')}
              </Badge>
            )}
          </p>
          <p className="tabular text-xs text-ink-faint">{p.id}</p>
        </div>
      ),
    },
    {
      key: 'reason',
      header: t('overview.failedPayments.reason'),
      hideBelow: 'sm',
      cell: (p) => (
        <div className="grid gap-1">
          <StatusPill kind="payment" status={p.status} size="sm" />
          <span className="text-xs text-ink-soft">
            {p.failureCode ? tryT(`failureCode.${p.failureCode}`, t('failureCode.unknown')) : '–'}
          </span>
        </div>
      ),
    },
    { key: 'amount', header: t('common.amount'), numeric: true, cell: (p) => <MoneyText value={p.amount} /> },
    { key: 'date', header: t('common.date'), hideBelow: 'md', cell: (p) => f.date(p.createdAt) },
  ]

  return (
    <>
      <PageHeader
        title={t('pages.overview.title')}
        description={t('pages.overview.description')}
        actions={
          <SegmentedControl
            label={t('overview.rangeLabel')}
            value={rangeKey}
            onChange={setRangeKey}
            options={[
              { value: 'd7', label: t('overview.range.d7') },
              { value: 'd30', label: t('overview.range.d30') },
              { value: 'd90', label: t('overview.range.d90') },
            ]}
          />
        }
      />

      <div className="grid grid-cols-[minmax(0,1fr)] gap-6">
        <section aria-label={t('overview.statsLabel')} className="grid grid-cols-[minmax(0,1fr)] gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard
            label={t('overview.stats.revenue')}
            icon={<Wallet />}
            tone="brand"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.totalRevenue} />}
            hint={s && t('overview.stats.revenueHint', { count: f.number(s.paymentCount) })}
          />
          <StatCard
            label={t('overview.stats.failed')}
            icon={<CircleAlert />}
            tone="danger"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.failedAmount} />}
            hint={s && t('overview.stats.countHint', { count: f.number(s.failedCount) })}
          />
          <StatCard
            label={t('overview.stats.pending')}
            icon={<Clock />}
            tone="warning"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.pendingAmount} />}
            hint={s && t('overview.stats.countHint', { count: f.number(s.pendingCount) })}
          />
          <StatCard
            label={t('overview.stats.recovered')}
            icon={<Undo2 />}
            tone="success"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.recoveredRevenue} />}
            hint={t('overview.stats.recoveredHint')}
          />
        </section>

        {s && s.excludedPaymentCount > 0 && (
          <p className="text-xs text-ink-soft">{t('overview.excluded', { count: f.number(s.excludedPaymentCount) })}</p>
        )}

        <div className="grid grid-cols-[minmax(0,1fr)] gap-6 xl:grid-cols-3">
          <Card className="xl:col-span-2">
            <CardHeader title={t('overview.trend.title')} description={t('overview.trend.description')} />
            {summary.isPending ? (
              <Skeleton className="h-[260px] w-full" />
            ) : s ? (
              <TrendChart
                ariaLabel={t('overview.trend.aria')}
                dates={s.series.map((p) => p.date)}
                series={[
                  { key: 'revenue', label: t('overview.trend.revenue'), color: 'var(--chart-1)', values: s.series.map((p) => p.revenue) },
                  { key: 'recovered', label: t('overview.trend.recovered'), color: 'var(--chart-2)', values: s.series.map((p) => p.recovered) },
                ]}
              />
            ) : (
              <EmptyState
                tone="danger"
                title={summaryError ?? t('errors.generic')}
                action={
                  <Button size="sm" variant="secondary" onClick={() => void summary.refetch()}>
                    {t('common.retry')}
                  </Button>
                }
              />
            )}
          </Card>

          <Card>
            <CardHeader title={t('overview.recovery.title')} description={t('overview.recovery.description')} />
            <Rows loading={recovery.isPending} />
            {recovery.isError && (
              <EmptyState
                tone="danger"
                title={errorMessage(recovery.error)}
                className="py-6"
                action={
                  <Button size="sm" variant="secondary" onClick={() => void recovery.refetch()}>
                    {t('common.retry')}
                  </Button>
                }
              />
            )}
            {recovery.data && !recovery.isError && (
              <>
                <dl>
                  <Row label={t('overview.recovery.lost')}>
                    <MoneyText value={recovery.data.lostAmount} />
                  </Row>
                  <Row label={t('overview.recovery.recovered')}>
                    <MoneyText value={recovery.data.recoveredAmount} />
                  </Row>
                  <Row label={t('overview.recovery.rate')}>
                    <span className="tabular">{f.percent(recovery.data.recoveryRate)}</span>
                  </Row>
                  <Row label={t('overview.recovery.open')}>
                    <span className="tabular">{f.number(recovery.data.openSuggestionCount)}</span>
                  </Row>
                </dl>
                <Link to="/recovery" className={buttonStyles({ variant: 'secondary', size: 'sm', className: 'mt-4 w-full' })}>
                  {t('overview.recovery.cta')}
                  <ArrowRight className="size-4" />
                </Link>
              </>
            )}
          </Card>
        </div>

        <div className="grid grid-cols-[minmax(0,1fr)] gap-6 xl:grid-cols-3">
          <Card className="xl:col-span-2">
            <CardHeader
              title={t('overview.failedPayments.title')}
              description={t('overview.failedPayments.description')}
              action={
                <Link to="/payments" className={buttonStyles({ variant: 'ghost', size: 'sm' })}>
                  {t('common.viewAll')}
                  <ArrowRight className="size-4" />
                </Link>
              }
            />
            <DataTable
              caption={t('overview.failedPayments.title')}
              columns={columns}
              rows={failed.data?.items}
              getRowId={(p) => p.id}
              loading={failed.isPending}
              fetching={failed.isFetching && !failed.isPending}
              error={failed.isError ? errorMessage(failed.error) : undefined}
              onRetry={() => void failed.refetch()}
              emptyTitle={t('overview.failedPayments.empty')}
            />
          </Card>

          <Card>
            <CardHeader title={t('overview.ai.title')} description={t('overview.ai.description')} action={<Sparkles className="size-4 text-brand-500" />} />
            <Rows loading={ai.isPending} />
            {ai.isError && (
              <EmptyState
                tone="danger"
                title={errorMessage(ai.error)}
                className="py-6"
                action={
                  <Button size="sm" variant="secondary" onClick={() => void ai.refetch()}>
                    {t('common.retry')}
                  </Button>
                }
              />
            )}
            {ai.data && !ai.isError && (
              <>
                <dl>
                  <Row label={t('overview.ai.cost')}>
                    <MoneyText value={ai.data.totalCost} />
                  </Row>
                  <Row label={t('overview.ai.requests')}>
                    <span className="tabular">{f.number(ai.data.totalRequests)}</span>
                  </Row>
                  <Row label={t('overview.ai.savings')}>
                    <MoneyText value={ai.data.savings} />
                  </Row>
                  <Row label={t('overview.ai.budget')}>
                    {ai.data.budget ? (
                      <span className="tabular">
                        <MoneyText value={ai.data.budget.spent} maximumFractionDigits={0} /> / <MoneyText value={ai.data.budget.limit} maximumFractionDigits={0} />
                      </span>
                    ) : (
                      <span className="font-normal text-ink-soft">{t('overview.ai.noBudget')}</span>
                    )}
                  </Row>
                </dl>
                <p className="mt-3 text-xs text-ink-faint">{t('overview.ai.savingsNote')}</p>
                <Link to="/ai-cost" className={buttonStyles({ variant: 'secondary', size: 'sm', className: 'mt-4 w-full' })}>
                  {t('overview.ai.cta')}
                  <ArrowRight className="size-4" />
                </Link>
              </>
            )}
          </Card>
        </div>
      </div>
    </>
  )
}
