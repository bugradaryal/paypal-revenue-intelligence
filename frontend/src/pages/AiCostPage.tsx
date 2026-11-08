import { useMemo, useState } from 'react'
import { Coins, Database, PiggyBank, Repeat2, Wallet } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button, Card, CardHeader, Skeleton, Tab, Tabs, TabsList, TabsPanel } from '@/components/ui'
import {
  DataTable,
  EmptyState,
  MoneyText,
  SegmentedControl,
  StatCard,
  TrendChart,
  type Column,
} from '@/components/patterns'
import { useAiBreakdown, useAiUsageSeries, useAiUsageSummary } from '@/features/ai-cost/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import type { AiCostBreakdownItem } from '@/services/types'

const RANGES = { d7: 7, d30: 30, d90: 90 } as const
type RangeKey = keyof typeof RANGES

/** AI costs are small amounts, so they show up to four decimals. */
const AI_DIGITS = 4

export function AiCostPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const [rangeKey, setRangeKey] = useState<RangeKey>('d30')
  const [dimension, setDimension] = useState<'model' | 'provider'>('model')
  const range = useMemo(() => lastDays(RANGES[rangeKey]), [rangeKey])

  const summary = useAiUsageSummary(range)
  const series = useAiUsageSeries(range)
  const breakdown = useAiBreakdown(range)

  const s = summary.isError ? undefined : summary.data
  const summaryError = summary.isError ? errorMessage(summary.error) : undefined
  const common = {
    loading: summary.isPending,
    error: summaryError,
    onRetry: () => void summary.refetch(),
  }

  const columns: Column<AiCostBreakdownItem>[] = [
    {
      key: 'name',
      header: dimension === 'model' ? t('aiCost.breakdown.model') : t('aiCost.breakdown.provider'),
      cell: (i) => <span className="font-medium">{i.label}</span>,
    },
    { key: 'requests', header: t('aiCost.breakdown.requests'), numeric: true, hideBelow: 'sm', cell: (i) => f.number(i.requests) },
    {
      key: 'tokens',
      header: t('aiCost.breakdown.tokens'),
      numeric: true,
      hideBelow: 'md',
      cell: (i) => (
        <span className="tabular">
          {f.number(i.inputTokens, { compact: true })} / {f.number(i.outputTokens, { compact: true })}
        </span>
      ),
    },
    { key: 'cost', header: t('aiCost.breakdown.cost'), numeric: true, cell: (i) => <MoneyText value={i.cost} maximumFractionDigits={AI_DIGITS} /> },
  ]

  const chartError = (query: { error: unknown; refetch: () => unknown }) => (
    <EmptyState
      tone="danger"
      title={errorMessage(query.error)}
      action={
        <Button size="sm" variant="secondary" onClick={() => void query.refetch()}>
          {t('common.retry')}
        </Button>
      }
    />
  )

  const rows = breakdown.isError ? undefined : dimension === 'model' ? breakdown.data?.byModel : breakdown.data?.byProvider

  return (
    <>
      <PageHeader
        title={t('pages.aiCost.title')}
        description={t('pages.aiCost.description')}
        actions={
          <SegmentedControl
            label={t('aiCost.rangeLabel')}
            value={rangeKey}
            onChange={setRangeKey}
            options={[
              { value: 'd7', label: t('aiCost.range.d7') },
              { value: 'd30', label: t('aiCost.range.d30') },
              { value: 'd90', label: t('aiCost.range.d90') },
            ]}
          />
        }
      />

      <div className="grid grid-cols-[minmax(0,1fr)] gap-6">
        <section aria-label={t('overview.statsLabel')} className="grid grid-cols-[minmax(0,1fr)] gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <StatCard
            label={t('aiCost.stats.cost')}
            icon={<Coins />}
            tone="brand"
            {...common}
            value={s && <MoneyText value={s.totalCost} maximumFractionDigits={AI_DIGITS} />}
            hint={s && t('aiCost.stats.costHint', { count: f.number(s.totalRequests) })}
          />
          <StatCard
            label={t('aiCost.stats.tokens')}
            icon={<Database />}
            tone="neutral"
            {...common}
            value={
              s && (
                <span className="tabular">
                  {f.number(s.inputTokens, { compact: true })} / {f.number(s.outputTokens, { compact: true })}
                </span>
              )
            }
            hint={t('aiCost.stats.tokensHint')}
          />
          <StatCard
            label={t('aiCost.stats.cache')}
            icon={<Repeat2 />}
            tone="neutral"
            {...common}
            value={s && <span className="tabular">{f.percent(s.cacheHitRate)}</span>}
            hint={t('aiCost.stats.cacheHint')}
          />
          <StatCard
            label={t('aiCost.stats.savings')}
            icon={<PiggyBank />}
            tone="success"
            {...common}
            value={s && <MoneyText value={s.savings} maximumFractionDigits={AI_DIGITS} />}
            hint={t('aiCost.stats.savingsHint')}
          />

          <Card padding="sm" className="min-w-0 p-5 sm:col-span-2">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-medium text-ink-soft">{t('aiCost.budget.title')}</p>
              <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-surface-muted text-ink-soft [&_svg]:size-4">
                <Wallet />
              </span>
            </div>
            {summary.isPending ? (
              <Skeleton className="mt-4 h-10 w-full" />
            ) : s?.budget ? (
              <div className="mt-3 grid gap-2">
                <p className="text-sm">
                  <MoneyText value={s.budget.spent} maximumFractionDigits={AI_DIGITS} className="text-2xl font-bold tracking-tight" />
                  <span className="text-ink-soft"> {t('aiCost.budget.of')} </span>
                  <MoneyText value={s.budget.limit} maximumFractionDigits={AI_DIGITS} className="font-semibold" />
                </p>
                {/* Visual scale only; the amounts above are what the backend reported. */}
                <div aria-hidden className="h-2 overflow-hidden rounded-full bg-surface-muted">
                  <div
                    className="h-full rounded-full bg-brand-500"
                    style={{ width: `${Math.min(100, (Number(s.budget.spent.amount) / Math.max(Number(s.budget.limit.amount), Number.EPSILON)) * 100)}%` }}
                  />
                </div>
                <p className="text-xs text-ink-soft">{t('aiCost.budget.hint')}</p>
              </div>
            ) : (
              <p className="mt-3 text-sm text-ink-soft">{summaryError ?? t('aiCost.budget.none')}</p>
            )}
          </Card>
        </section>

        <div className="grid grid-cols-[minmax(0,1fr)] gap-6 xl:grid-cols-2">
          <Card>
            <CardHeader title={t('aiCost.trend.costTitle')} description={t('aiCost.trend.description')} />
            {series.isPending ? (
              <Skeleton className="h-[260px] w-full" />
            ) : series.data && !series.isError ? (
              <TrendChart
                ariaLabel={t('aiCost.trend.costAria')}
                dates={series.data.map((p) => p.date)}
                moneyDigits={AI_DIGITS}
                series={[{ key: 'cost', label: t('aiCost.trend.costTitle'), color: 'var(--chart-1)', values: series.data.map((p) => p.cost) }]}
              />
            ) : (
              chartError(series)
            )}
          </Card>

          <Card>
            <CardHeader title={t('aiCost.trend.requestsTitle')} description={t('aiCost.trend.description')} />
            {series.isPending ? (
              <Skeleton className="h-[260px] w-full" />
            ) : series.data && !series.isError ? (
              <TrendChart
                ariaLabel={t('aiCost.trend.requestsAria')}
                dates={series.data.map((p) => p.date)}
                series={[{ key: 'requests', label: t('aiCost.trend.requestsTitle'), color: 'var(--ink-soft)', values: series.data.map((p) => p.requests) }]}
              />
            ) : (
              chartError(series)
            )}
          </Card>
        </div>

        <Card>
          <CardHeader title={t('aiCost.breakdown.title')} description={t('aiCost.breakdown.description')} />
          <Tabs value={dimension} onValueChange={(v) => setDimension(v as 'model' | 'provider')}>
            <TabsList label={t('aiCost.breakdown.title')}>
              <Tab value='model'>{t('aiCost.breakdown.model')}</Tab>
              <Tab value="provider">{t('aiCost.breakdown.provider')}</Tab>
            </TabsList>
            <TabsPanel value={dimension} className="mt-4">
              <DataTable
                caption={t('aiCost.breakdown.title')}
                columns={columns}
                rows={rows}
                getRowId={(i) => i.key}
                loading={breakdown.isPending}
                fetching={breakdown.isFetching && !breakdown.isPending}
                error={breakdown.isError ? errorMessage(breakdown.error) : undefined}
                onRetry={() => void breakdown.refetch()}
                skeletonRows={3}
                emptyTitle={t('aiCost.breakdown.empty')}
              />
            </TabsPanel>
          </Tabs>
        </Card>
      </div>
    </>
  )
}
