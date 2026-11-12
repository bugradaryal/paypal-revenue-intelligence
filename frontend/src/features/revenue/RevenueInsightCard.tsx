import { ArrowDownRight, ArrowUpRight, Minus, Sparkles } from 'lucide-react'
import { Button, Card, Badge, Skeleton } from '@/components/ui'
import { EmptyState, MoneyText } from '@/components/patterns'
import { env } from '@/config/env'
import { useGenerateRevenueInsight, useRevenueInsights } from '@/features/revenue/queries'
import { LOCALES, useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import type { RevenueInsight } from '@/services/types'

interface RevenueInsightCardProps {
  days: number
}

function Change({ value, lowerIsBetter = false }: { value: number | null; lowerIsBetter?: boolean }) {
  const { t } = useI18n()
  const f = useFormat()
  if (value === null) return <span className="text-ink-faint">{t('overview.insight.noComparison')}</span>

  const Icon = value === 0 ? Minus : value > 0 ? ArrowUpRight : ArrowDownRight
  const favorable = value !== 0 && (lowerIsBetter ? value < 0 : value > 0)
  const tone = value === 0 ? 'text-ink-soft' : favorable ? 'text-success' : 'text-danger'

  return (
    <span className={`inline-flex items-center gap-1 tabular ${tone}`}>
      <Icon aria-hidden="true" className="size-3.5" />
      {value > 0 ? '+' : ''}{f.percent(value / 100)}
    </span>
  )
}

function InsightMetrics({ insight }: { insight: RevenueInsight }) {
  const { t } = useI18n()
  const f = useFormat()
  const metrics = insight.metrics
  const tiles = [
    { label: t('overview.insight.revenue'), value: <MoneyText value={metrics.revenue} /> },
    { label: t('overview.insight.atRisk'), value: <MoneyText value={metrics.failedAmount} /> },
    { label: t('overview.insight.failureRate'), value: f.percent(metrics.failureRate) },
    { label: t('overview.insight.recoveryRate'), value: f.percent(metrics.recoveryRate) },
  ]

  return (
    <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
      {tiles.map((tile) => (
        <div key={tile.label} className="rounded-xl border border-line/70 bg-surface-card/80 p-3.5">
          <p className="text-xs text-ink-soft">{tile.label}</p>
          <p className="mt-1.5 text-lg font-bold tabular tracking-tight">{tile.value}</p>
        </div>
      ))}
    </div>
  )
}

export function RevenueInsightCard({ days }: RevenueInsightCardProps) {
  const { t, tryT, locale } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const query = useRevenueInsights({ page: 1, pageSize: 1 })
  const generate = useGenerateRevenueInsight()
  const insight = query.data?.items[0]
  const commentaryLanguage = insight
    ? LOCALES.find((language) => language.code === insight.commentaryLanguage)?.label ?? insight.commentaryLanguage
    : undefined

  const periodEnd = insight
    ? new Date(Date.parse(insight.periodTo) - 1).toISOString()
    : undefined
  const sourceLabel = env.isMock
    ? t('overview.insight.sourceDemo')
    : insight?.commentarySource === 'ai'
      ? t('overview.insight.sourceAi')
      : t('overview.insight.sourceRule')

  return (
    <Card className="relative isolate overflow-hidden border-brand-300/40 bg-surface-gradient shadow-lg">
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden rounded-[inherit]">
        <div className="hero-grid absolute inset-0 opacity-25" />
        <div className="absolute -right-16 -top-24 size-72 rounded-full bg-brand-400/20 blur-[90px] motion-safe:animate-float" />
        <div className="absolute -bottom-36 left-1/4 size-72 rounded-full bg-success/10 blur-[90px] motion-safe:animate-float" />
        <div className="absolute -right-12 top-2 size-56 rounded-full border border-brand-300/20 motion-safe:animate-orbit">
          <span className="absolute left-7 top-8 size-2 rounded-full bg-brand-300 shadow-[0_0_18px_5px_rgb(242_140_69_/_0.45)]" />
        </div>
        <div className="absolute inset-y-0 -left-1/2 w-2/3 bg-gradient-to-r from-transparent via-brand-300/10 to-transparent motion-safe:animate-insight-sweep" />
      </div>
      <div className="relative">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <span className="grid size-8 place-items-center rounded-xl bg-brand-soft text-brand-text">
                <Sparkles aria-hidden="true" className="size-4" />
              </span>
              <h2 className="text-base font-bold tracking-tight">{t('overview.insight.title')}</h2>
              <Badge dot tone={env.isMock ? 'warning' : insight?.commentarySource === 'ai' ? 'brand' : 'neutral'}>
                {sourceLabel}
              </Badge>
            </div>
            <p className="mt-2 text-sm text-ink-soft">{t('overview.insight.description')}</p>
          </div>
          <Button
            className="shrink-0"
            leadingIcon={<Sparkles className="size-4" />}
            loading={generate.isPending}
            onClick={() => generate.mutate(days)}
          >
            {generate.isPending ? t('overview.insight.generating') : t('overview.insight.generate')}
          </Button>
        </div>

        {generate.isError && (
          <p role="alert" className="mt-4 rounded-lg border border-danger/20 bg-danger-soft px-3 py-2 text-sm text-danger">
            {errorMessage(generate.error)}
          </p>
        )}

        {query.isPending ? (
          <div className="mt-6 grid gap-4">
            <Skeleton className="h-16 w-full" />
            <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
              {[0, 1, 2, 3].map((item) => <Skeleton key={item} className="h-20 w-full" />)}
            </div>
          </div>
        ) : query.isError && !insight ? (
          <EmptyState
            tone="danger"
            className="mt-4"
            title={errorMessage(query.error)}
            action={
              <Button size="sm" variant="secondary" onClick={() => void query.refetch()}>
                {t('common.retry')}
              </Button>
            }
          />
        ) : !insight ? (
          <EmptyState
            icon={<Sparkles />}
            className="mt-4"
            title={t('overview.insight.emptyTitle')}
            description={t('overview.insight.emptyHint')}
          />
        ) : (
          <div className="mt-5 grid gap-4">
            <div className="rounded-xl border border-brand-200/60 bg-brand-soft/50 p-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-xs font-semibold uppercase tracking-wide text-brand-text">{t('overview.insight.summary')}</p>
                {periodEnd && (
                  <p className="text-xs text-ink-faint">
                    {t('overview.insight.period', { from: f.date(insight.periodFrom), to: f.date(periodEnd) })}
                  </p>
                )}
              </div>
              <p className="mt-2 text-sm leading-6 text-ink">{insight.commentary}</p>
              {insight.commentaryLanguage !== locale && commentaryLanguage && (
                <p className="mt-2 text-xs text-ink-faint">
                  {t('overview.insight.writtenIn', { language: commentaryLanguage })}
                </p>
              )}
            </div>

            <InsightMetrics insight={insight} />

            <div className="grid gap-4 lg:grid-cols-2">
              <section className="rounded-xl border border-line/70 bg-surface-card/70 p-4" aria-label={t('overview.insight.topFailures')}>
                <h3 className="text-xs font-semibold uppercase tracking-wide text-ink-soft">{t('overview.insight.topFailures')}</h3>
                {insight.metrics.topFailureReasons.length ? (
                  <ul className="mt-3 grid gap-2">
                    {insight.metrics.topFailureReasons.map((reason) => (
                      <li key={reason.code} className="flex items-center justify-between gap-3 text-sm">
                        <span className="min-w-0 truncate">
                          {tryT(`failureCode.${reason.code}`, reason.code.replace(/_/g, ' '))}
                          <span className="ml-2 text-xs text-ink-faint">×{f.number(reason.count)}</span>
                        </span>
                        <MoneyText value={reason.amount} className="shrink-0 font-semibold" />
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="mt-3 text-sm text-ink-faint">{t('overview.insight.noFailures')}</p>
                )}
              </section>

              <section className="rounded-xl border border-line/70 bg-surface-card/70 p-4" aria-label={t('overview.insight.previousPeriod')}>
                <h3 className="text-xs font-semibold uppercase tracking-wide text-ink-soft">{t('overview.insight.previousPeriod')}</h3>
                <dl className="mt-2 grid gap-2">
                  <div className="flex items-center justify-between gap-3 text-sm">
                    <dt className="text-ink-soft">{t('overview.insight.revenueChange')}</dt>
                    <dd><Change value={insight.metrics.previous.revenueChangePercent} /></dd>
                  </div>
                  <div className="flex items-center justify-between gap-3 text-sm">
                    <dt className="text-ink-soft">{t('overview.insight.failedChange')}</dt>
                    <dd><Change value={insight.metrics.previous.failedAmountChangePercent} lowerIsBetter /></dd>
                  </div>
                </dl>
              </section>
            </div>

            <div className="flex flex-col gap-1 border-t border-line/70 pt-3 text-xs text-ink-faint sm:flex-row sm:items-center sm:justify-between">
              {env.isMock && <p>{t('overview.insight.demoNotice')}</p>}
              <p>{t('overview.insight.updated', { date: f.dateTime(insight.createdAt) })}</p>
            </div>
          </div>
        )}
      </div>
    </Card>
  )
}
