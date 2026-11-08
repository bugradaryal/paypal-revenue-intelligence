import { useMemo, useRef, useState } from 'react'
import { CircleAlert, Percent, Sparkles, Undo2, Wand2 } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button, Card, CardHeader, Select, Skeleton, useToast } from '@/components/ui'
import {
  ApprovalBanner,
  ChipFilter,
  EmptyState,
  FilterBar,
  MoneyText,
  Pagination,
  SegmentedControl,
  StatCard,
} from '@/components/patterns'
import { SuggestionCard } from '@/features/recovery/SuggestionCard'
import { useSuggestionActions } from '@/features/recovery/useSuggestionActions'
import { useGenerateSuggestions, useRecoverySummary, useSuggestions } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import type { RecoverySuggestionStatus } from '@/services/types'

const PAGE_SIZE = 6
const RANGES = { d7: 7, d30: 30, d90: 90 } as const
type RangeKey = keyof typeof RANGES
const STATUSES: RecoverySuggestionStatus[] = ['proposed', 'approved', 'executed', 'rejected', 'failed']
type SortKey = 'newest' | 'value'

export function RecoveryPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const listRef = useRef<HTMLDivElement>(null)

  const [rangeKey, setRangeKey] = useState<RangeKey>('d30')
  const [statuses, setStatuses] = useState<RecoverySuggestionStatus[]>(['proposed'])
  const [sortKey, setSortKey] = useState<SortKey>('newest')
  const [page, setPage] = useState(1)

  const range = useMemo(() => lastDays(RANGES[rangeKey]), [rangeKey])
  const summary = useRecoverySummary(range)
  const suggestions = useSuggestions({
    page,
    pageSize: PAGE_SIZE,
    status: statuses.length ? statuses : undefined,
    sortBy: sortKey === 'value' ? 'expectedRecovery' : 'createdAt',
    sortDir: 'desc',
  })
  const actions = useSuggestionActions()
  const generate = useGenerateSuggestions()

  const s = summary.data
  const summaryError = summary.isError ? errorMessage(summary.error) : undefined
  const items = suggestions.data?.items

  function onGenerate() {
    generate.mutate(undefined, {
      onSuccess: ({ createdCount }) =>
        toast({
          tone: createdCount > 0 ? 'success' : 'info',
          title: createdCount > 0 ? t('recovery.toast.generated', { count: f.number(createdCount) }) : t('recovery.toast.generatedNone'),
        }),
      onError: (error) => toast({ tone: 'danger', title: errorMessage(error) }),
    })
  }

  function reviewOpen() {
    setStatuses(['proposed'])
    setPage(1)
    listRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  const open = s?.openSuggestionCount ?? 0

  return (
    <>
      <PageHeader
        title={t('pages.recovery.title')}
        description={t('pages.recovery.description')}
        actions={
          <SegmentedControl
            label={t('recovery.rangeLabel')}
            value={rangeKey}
            onChange={setRangeKey}
            options={[
              { value: 'd7', label: t('recovery.range.d7') },
              { value: 'd30', label: t('recovery.range.d30') },
              { value: 'd90', label: t('recovery.range.d90') },
            ]}
          />
        }
      />

      <div className="grid grid-cols-[minmax(0,1fr)] gap-6">
        {open > 0 && (
          <ApprovalBanner
            title={t('recovery.banner.title', { count: f.number(open) })}
            description={t('recovery.banner.description')}
            action={
              <Button size="sm" variant="secondary" onClick={reviewOpen}>
                {t('recovery.banner.review')}
              </Button>
            }
          />
        )}

        <section aria-label={t('overview.statsLabel')} className="grid grid-cols-[minmax(0,1fr)] gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard
            label={t('recovery.stats.lost')}
            icon={<CircleAlert />}
            tone="danger"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.lostAmount} />}
            hint={t('recovery.stats.lostHint')}
          />
          <StatCard
            label={t('recovery.stats.recovered')}
            icon={<Undo2 />}
            tone="success"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <MoneyText value={s.recoveredAmount} />}
            hint={t('recovery.stats.recoveredHint')}
          />
          <StatCard
            label={t('recovery.stats.rate')}
            icon={<Percent />}
            tone="brand"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <span className="tabular">{f.percent(s.recoveryRate)}</span>}
            hint={t('recovery.stats.rateHint')}
          />
          <StatCard
            label={t('recovery.stats.open')}
            icon={<Sparkles />}
            tone="warning"
            loading={summary.isPending}
            error={summaryError}
            onRetry={() => void summary.refetch()}
            value={s && <span className="tabular">{f.number(s.openSuggestionCount)}</span>}
            hint={t('recovery.stats.openHint')}
          />
        </section>

        <div ref={listRef} className="scroll-mt-24">
          <Card className="grid grid-cols-[minmax(0,1fr)] gap-5">
            <CardHeader
              className="mb-0"
              title={t('recovery.list.title')}
              description={t('recovery.list.description')}
              action={
                <Button
                  size="sm"
                  leadingIcon={<Wand2 className="size-4" />}
                  loading={generate.isPending}
                  onClick={onGenerate}
                >
                  {generate.isPending ? t('recovery.generating') : t('recovery.generate')}
                </Button>
              }
            />
            {generate.isPending && <p className="-mt-2 text-xs text-ink-soft">{t('recovery.generatingHint')}</p>}

            <FilterBar
              canReset={statuses.length !== 1 || statuses[0] !== 'proposed'}
              onReset={() => {
                setStatuses(['proposed'])
                setPage(1)
              }}
            >
              <ChipFilter
                label={t('payments.statusLabel')}
                value={statuses}
                onChange={(value) => {
                  setStatuses(value)
                  setPage(1)
                }}
                options={STATUSES.map((status) => ({ value: status, label: t(`suggestionStatus.${status}`) }))}
              />
              <div className="w-full sm:ml-auto sm:w-60">
                <Select
                  aria-label={t('recovery.sortLabel')}
                  value={sortKey}
                  onChange={(e) => {
                    setSortKey(e.target.value as SortKey)
                    setPage(1)
                  }}
                  options={[
                    { value: 'newest', label: t('recovery.sort.newest') },
                    { value: 'value', label: t('recovery.sort.value') },
                  ]}
                />
              </div>
            </FilterBar>

            {suggestions.isPending ? (
              <div className="grid gap-4">
                {[0, 1, 2].map((i) => (
                  <Skeleton key={i} className="h-40 w-full" />
                ))}
              </div>
            ) : suggestions.isError && !items?.length ? (
              <EmptyState
                tone="danger"
                title={errorMessage(suggestions.error)}
                action={
                  <Button size="sm" variant="secondary" onClick={() => void suggestions.refetch()}>
                    {t('common.retry')}
                  </Button>
                }
              />
            ) : items && items.length === 0 ? (
              <EmptyState
                icon={<Sparkles />}
                title={statuses.length ? t('recovery.list.emptyFiltered') : t('recovery.list.empty')}
                description={t('recovery.list.emptyHint')}
              />
            ) : (
              <div className="grid grid-cols-[minmax(0,1fr)] gap-4">
                {items?.map((suggestion) => (
                  <SuggestionCard
                    key={suggestion.id}
                    suggestion={suggestion}
                    deciding={actions.deciding(suggestion.id)}
                    busy={actions.busy}
                    onApprove={actions.onApprove}
                    onReject={actions.onReject}
                  />
                ))}
              </div>
            )}

            {suggestions.data && (
              <Pagination
                page={suggestions.data.page}
                pageSize={suggestions.data.pageSize}
                totalItems={suggestions.data.totalItems}
                onPageChange={setPage}
              />
            )}
          </Card>
        </div>
      </div>

      {actions.confirmDialog}
    </>
  )
}
