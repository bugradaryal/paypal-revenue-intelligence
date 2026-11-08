import type { ReactNode } from 'react'
import { useState } from 'react'
import { useQueryClient, type UseQueryResult } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'
import { Badge, Button, Card, CardHeader, Skeleton, Switch } from '@/components/ui'
import { env } from '@/config/env'
import { hasMockOperations } from '@/services/adapters'
import { useAiBreakdown, useAiUsageSeries, useAiUsageSummary } from '@/features/ai-cost/queries'
import {
  useDecideSuggestion,
  usePayments,
  useRecoverySummary,
  useRevenueSummary,
  useSuggestions,
} from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import { setMockFailure } from '@/services/adapters/mock'
import { ContractNotReadyError } from '@/services/errors'
import { DevHeader } from './DevHeader'

/**
 * Internal developer page: shows that the data flow (adapter -> TanStack Query -> UI) works,
 * including loading, error and mutation states. Not part of the product UI.
 */

function QueryCard<T>({ title, query, children }: { title: string; query: UseQueryResult<T>; children: (data: T) => ReactNode }) {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const state = query.isPending ? 'Loading' : query.isError ? 'Error' : query.isFetching ? 'Refreshing' : 'Ready'
  const tone = state === 'Ready' ? 'success' : state === 'Error' ? 'danger' : 'info'

  return (
    <Card>
      <CardHeader
        title={title}
        action={
          <Badge tone={tone} dot>
            {t(`dev.data.state${state}`)}
          </Badge>
        }
      />
      {query.isPending && (
        <div className="grid gap-2">
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      )}
      {query.isError && (
        <div className="grid gap-1 text-sm">
          <p className="font-semibold text-danger">{errorMessage(query.error)}</p>
          {/* The class name is a code identifier, shown for developers. */}
          <p className="tabular text-xs text-ink-faint">{query.error.name}</p>
          {query.error instanceof ContractNotReadyError && (
            <p className="text-xs text-ink-faint">{t('dev.data.notReadyHint')}</p>
          )}
        </div>
      )}
      {query.data !== undefined && (
        <div className="grid gap-3">
          <div className="text-sm">{children(query.data)}</div>
          <details className="text-xs">
            <summary className="cursor-pointer select-none text-ink-soft hover:text-ink">{t('dev.data.json')}</summary>
            <pre className="tabular mt-2 max-h-64 overflow-auto rounded-md bg-surface-muted p-3 text-[11px] leading-relaxed">
              {JSON.stringify(query.data, null, 2)}
            </pre>
          </details>
        </div>
      )}
    </Card>
  )
}

function Stat({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-line py-1.5 last:border-0">
      <span className="text-ink-soft">{label}</span>
      <span className="tabular min-w-0 text-right [overflow-wrap:anywhere]">{value}</span>
    </div>
  )
}

export function DataPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const queryClient = useQueryClient()
  const [failing, setFailing] = useState(false)
  const range = lastDays(30)

  const summary = useRevenueSummary(range)
  const payments = usePayments({ page: 1, pageSize: 5, status: ['failed'] })
  const recovery = useRecoverySummary(range)
  const suggestions = useSuggestions({ page: 1, pageSize: 4, status: ['proposed'] })
  const aiSummary = useAiUsageSummary(range)
  const aiSeries = useAiUsageSeries(range)
  const aiBreakdown = useAiBreakdown(range)
  const decide = useDecideSuggestion()

  function toggleFailure(next: boolean) {
    setFailing(next)
    setMockFailure(next)
    void queryClient.invalidateQueries()
  }

  return (
    <div className="min-h-dvh bg-surface-app">
      <DevHeader current="data" />

      <main className="mx-auto grid max-w-6xl grid-cols-[minmax(0,1fr)] gap-6 px-4 py-10 sm:px-6">
        <div>
          <p className="tabular text-xs font-medium uppercase tracking-widest text-brand-text">{t('dev.data.eyebrow')}</p>
          <h1 className="mt-2 text-3xl font-bold tracking-tight">{t('dev.data.title')}</h1>
          <p className="mt-3 max-w-2xl text-ink-soft">{t('dev.data.intro')}</p>
        </div>

        <Card className="grid grid-cols-[minmax(0,1fr)] gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <div className="grid gap-1 text-sm">
            <Stat label={t('dev.data.source')} value={env.dataSource} />
            <Stat label={t('dev.data.baseUrl')} value={env.apiBaseUrl} />
          </div>
          <div className="grid content-between gap-4">
            {hasMockOperations ? (
              <Switch
                checked={failing}
                onCheckedChange={toggleFailure}
                label={t('dev.data.simulate')}
                description={t('dev.data.simulateHint')}
              />
            ) : (
              <p className="text-sm text-ink-soft">{t('dev.data.realActive')}</p>
            )}
            <div>
              <Button
                variant="secondary"
                size="sm"
                leadingIcon={<RefreshCw className="size-4" />}
                onClick={() => void queryClient.invalidateQueries()}
              >
                {t('dev.data.refetch')}
              </Button>
            </div>
          </div>
        </Card>

        <div className="grid grid-cols-[minmax(0,1fr)] gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <QueryCard title={t('dev.data.cardSummary')} query={summary}>
            {(d) => (
              <>
                <Stat label={t('dev.data.revenue')} value={f.money(d.totalRevenue)} />
                <Stat label={t('dev.data.failed')} value={`${f.money(d.failedAmount)} · ${d.failedCount}`} />
                <Stat label={t('dev.data.pending')} value={`${f.money(d.pendingAmount)} · ${d.pendingCount}`} />
                <Stat label={t('dev.data.recovered')} value={f.money(d.recoveredRevenue)} />
                <Stat label={t('dev.data.chartPoints')} value={d.series.length} />
              </>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardPayments')} query={payments}>
            {(d) => (
              <>
                <Stat label={t('dev.data.totalFailed')} value={d.totalItems} />
                {d.items.map((p) => (
                  <Stat
                    key={p.id}
                    label={`${p.id} · ${p.customerName}`}
                    value={`${f.money(p.amount)} · ${f.date(p.createdAt)}`}
                  />
                ))}
              </>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardRecovery')} query={recovery}>
            {(d) => (
              <>
                <Stat label={t('dev.data.lost')} value={f.money(d.lostAmount)} />
                <Stat label={t('dev.data.recovered')} value={f.money(d.recoveredAmount)} />
                <Stat label={t('dev.data.recoveryRate')} value={f.percent(d.recoveryRate)} />
                <Stat label={t('dev.data.openSuggestions')} value={d.openSuggestionCount} />
              </>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardSuggestions')} query={suggestions}>
            {(d) => (
              <div className="grid min-w-0 gap-3">
                {d.items.length === 0 && <p className="text-ink-soft">{t('dev.data.noSuggestions')}</p>}
                {d.items.map((s) => (
                  <div key={s.id} className="grid min-w-0 gap-2 rounded-md border border-line p-3">
                    <div className="flex flex-wrap items-start justify-between gap-x-3 gap-y-1">
                      <p className="font-medium">
                        {s.customerName} <span className="tabular text-xs text-ink-faint">{s.id}</span>
                      </p>
                      <Badge tone="brand">{t(`suggestionType.${s.type}`)}</Badge>
                    </div>
                    {/* Written by the AI on the backend; shown as received. */}
                    <p className="text-xs text-ink-soft">{s.rationale}</p>
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        loading={decide.isPending && decide.variables?.id === s.id}
                        onClick={() => decide.mutate({ id: s.id, decision: 'approve' })}
                      >
                        {t('common.approve')}
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={decide.isPending}
                        onClick={() => decide.mutate({ id: s.id, decision: 'reject' })}
                      >
                        {t('common.reject')}
                      </Button>
                    </div>
                  </div>
                ))}
                {decide.isError && <p className="text-xs text-danger">{errorMessage(decide.error)}</p>}
              </div>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardAiSummary')} query={aiSummary}>
            {(d) => (
              <>
                <Stat label={t('dev.data.cost')} value={f.money(d.totalCost)} />
                <Stat label={t('dev.data.requests')} value={f.number(d.totalRequests)} />
                <Stat
                  label={t('dev.data.tokens')}
                  value={`${f.number(d.inputTokens, { compact: true })} / ${f.number(d.outputTokens, { compact: true })}`}
                />
                <Stat label={t('dev.data.cacheHit')} value={f.percent(d.cacheHitRate)} />
                <Stat label={t('dev.data.savings')} value={f.money(d.savings)} />
                <Stat label={t('dev.data.budget')} value={d.budget ? `${f.money(d.budget.spent)} / ${f.money(d.budget.limit)}` : '–'} />
              </>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardAiBreakdown')} query={aiBreakdown}>
            {(d) => (
              <>
                {d.byProvider.map((i) => (
                  <Stat key={i.key} label={`${t('dev.data.provider')} · ${i.label}`} value={f.money(i.cost)} />
                ))}
                {d.byModel.map((i) => (
                  <Stat key={i.key} label={`${t('dev.data.model')} · ${i.label}`} value={f.money(i.cost)} />
                ))}
              </>
            )}
          </QueryCard>

          <QueryCard title={t('dev.data.cardAiSeries')} query={aiSeries}>
            {(d) => {
              const first = d[0]
              const last = d[d.length - 1]
              return (
                <>
                  <Stat label={t('dev.data.points')} value={d.length} />
                  <Stat label={t('dev.data.firstDay')} value={first ? `${f.date(first.date)} · ${f.money(first.cost)}` : '–'} />
                  <Stat label={t('dev.data.lastDay')} value={last ? `${f.date(last.date)} · ${f.money(last.cost)}` : '–'} />
                </>
              )
            }}
          </QueryCard>
        </div>
      </main>
    </div>
  )
}
