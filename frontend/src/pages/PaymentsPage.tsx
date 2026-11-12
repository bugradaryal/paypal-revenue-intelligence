import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { AlertTriangle, RefreshCw } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button, Card, Dialog, Skeleton } from '@/components/ui'
import {
  ChipFilter,
  DataTable,
  EmptyState,
  FilterBar,
  MoneyText,
  SearchField,
  SegmentedControl,
  StatusPill,
  type Column,
  type SortState,
} from '@/components/patterns'
import { usePayment, usePayments } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { lastDays } from '@/lib/dates'
import type { Payment, PaymentStatus } from '@/services/types'

/**
 * Filters, sorting, page and the open payment live in the URL, so a reload or a shared link
 * shows exactly the same view.
 */

const PAGE_SIZE = 20
const STATUSES: PaymentStatus[] = ['completed', 'pending', 'failed', 'refunded']
const RANGES = { d7: 7, d30: 30, d90: 90, all: null } as const
type RangeKey = keyof typeof RANGES

const isStatus = (v: string): v is PaymentStatus => (STATUSES as string[]).includes(v)
const isRange = (v: string | null): v is RangeKey => v !== null && v in RANGES

function DetailRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="grid gap-0.5 border-b border-line py-3 text-sm last:border-0 sm:grid-cols-[10rem_minmax(0,1fr)] sm:gap-4">
      <dt className="text-ink-soft">{label}</dt>
      <dd className="min-w-0 [overflow-wrap:anywhere]">{children}</dd>
    </div>
  )
}

function PaymentDrawer({ id, onClose }: { id: string | null; onClose: () => void }) {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const query = usePayment(id ?? undefined)
  const p = query.data

  return (
    <Dialog
      variant="drawer"
      open={id !== null}
      onClose={onClose}
      title={p?.id ?? id ?? t('payments.detail.title')}
      description={t('payments.detail.title')}
    >
      {query.isPending && id && (
        <div className="grid gap-3">
          {[0, 1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className="h-5 w-full" />
          ))}
        </div>
      )}

      {query.isError && (
        <EmptyState
          tone="danger"
          icon={<AlertTriangle />}
          title={errorMessage(query.error)}
          action={
            <Button size="sm" variant="secondary" leadingIcon={<RefreshCw className="size-4" />} onClick={() => void query.refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      )}

      {p && (
        <div className="grid gap-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <MoneyText value={p.amount} className="text-2xl font-bold" />
            <StatusPill kind="payment" status={p.status} />
          </div>

          <dl>
            <DetailRow label={t('common.customer')}>{p.customerName}</DetailRow>
            <DetailRow label={t('payments.detail.customerId')}>
              <span className="tabular text-xs">{p.customerId}</span>
            </DetailRow>
            {p.failureCode && (
              <DetailRow label={t('payments.detail.reason')}>
                {tryT(`failureCode.${p.failureCode}`, t('failureCode.unknown'))}
              </DetailRow>
            )}
            <DetailRow label={t('payments.detail.transaction')}>
              {p.paypalTransactionId ? (
                <span className="tabular text-xs">{p.paypalTransactionId}</span>
              ) : (
                <span className="text-ink-soft">{t('payments.detail.noTransaction')}</span>
              )}
            </DetailRow>
            <DetailRow label={t('payments.detail.created')}>{f.dateTime(p.createdAt)}</DetailRow>
            <DetailRow label={t('payments.detail.updated')}>{f.dateTime(p.updatedAt)}</DetailRow>
          </dl>

          <section>
            <h3 className="mb-3 text-sm font-semibold">{t('payments.detail.history')}</h3>
            <ol className="grid gap-3 border-l border-line pl-4">
              {p.statusHistory.map((h, i) => (
                <li key={`${h.status}-${h.occurredAt}-${i}`} className="relative grid justify-items-start gap-1">
                  <span aria-hidden className="absolute -left-[1.3rem] top-1.5 size-2 rounded-full bg-brand-500" />
                  <StatusPill kind="payment" status={h.status} size="sm" />
                  <span className="text-xs text-ink-soft">{f.dateTime(h.occurredAt)}</span>
                </li>
              ))}
            </ol>
          </section>
        </div>
      )}
    </Dialog>
  )
}

export function PaymentsPage() {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const [params, setParams] = useSearchParams()

  const search = params.get('q') ?? ''
  const statuses = params.getAll('status').filter(isStatus)
  const rangeKey: RangeKey = isRange(params.get('range')) ? (params.get('range') as RangeKey) : 'd30'
  const sort: SortState = {
    by: params.get('sort') === 'amount' ? 'amount' : 'createdAt',
    dir: params.get('dir') === 'asc' ? 'asc' : 'desc',
  }
  const page = Math.max(1, Number(params.get('page')) || 1)
  const selectedId = params.get('id')

  function update(change: (next: URLSearchParams) => void, resetPage = true) {
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        change(next)
        if (resetPage) next.delete('page')
        return next
      },
      { replace: true },
    )
  }

  // The text box is local so typing is instant; the URL (and the request) follow after a short pause.
  const [draft, setDraft] = useState(search)
  useEffect(() => setDraft(search), [search])
  useEffect(() => {
    if (draft.trim() === search) return
    const timer = window.setTimeout(
      () =>
        update((next) => {
          if (draft.trim()) next.set('q', draft.trim())
          else next.delete('q')
        }),
      300,
    )
    return () => window.clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draft, search])

  const range = useMemo(() => (RANGES[rangeKey] === null ? undefined : lastDays(RANGES[rangeKey])), [rangeKey])

  const query = usePayments({
    page,
    pageSize: PAGE_SIZE,
    status: statuses.length ? statuses : undefined,
    search: search || undefined,
    range,
    sortBy: sort.by as 'createdAt' | 'amount',
    sortDir: sort.dir,
  })

  const filtersActive = Boolean(search) || statuses.length > 0 || rangeKey !== 'd30'

  const columns: Column<Payment>[] = [
    {
      key: 'payment',
      header: t('payments.columns.payment'),
      cell: (p) => (
        <div className="min-w-0">
          <p className="truncate font-medium">{p.customerName}</p>
          <p className="tabular text-xs text-ink-faint">{p.id}</p>
        </div>
      ),
    },
    { key: 'status', header: t('common.status'), cell: (p) => <StatusPill kind="payment" status={p.status} size="sm" /> },
    {
      key: 'reason',
      header: t('payments.columns.reason'),
      hideBelow: 'md',
      cell: (p) =>
        p.failureCode ? (
          <span className="text-ink-soft">{tryT(`failureCode.${p.failureCode}`, t('failureCode.unknown'))}</span>
        ) : (
          <span className="text-ink-faint">–</span>
        ),
    },
    { key: 'amount', header: t('common.amount'), numeric: true, sortKey: 'amount', cell: (p) => <MoneyText value={p.amount} /> },
    { key: 'date', header: t('common.date'), sortKey: 'createdAt', hideBelow: 'sm', cell: (p) => f.date(p.createdAt) },
  ]

  return (
    <>
      <PageHeader title={t('pages.payments.title')} description={t('pages.payments.description')} />

      <Card className="grid grid-cols-[minmax(0,1fr)] gap-5">
        <FilterBar
          canReset={filtersActive}
          onReset={() => {
            setDraft('')
            update((next) => {
              for (const key of ['q', 'status', 'range']) next.delete(key)
            })
          }}
        >
          <SearchField
            value={draft}
            onChange={setDraft}
            label={t('payments.searchLabel')}
            placeholder={t('payments.searchPlaceholder')}
          />
          <SegmentedControl
            label={t('payments.rangeLabel')}
            value={rangeKey}
            onChange={(value) => update((next) => (value === 'd30' ? next.delete('range') : next.set('range', value)))}
            options={[
              { value: 'd7', label: t('payments.range.d7') },
              { value: 'd30', label: t('payments.range.d30') },
              { value: 'd90', label: t('payments.range.d90') },
              { value: 'all', label: t('payments.range.all') },
            ]}
          />
          <ChipFilter
            label={t('payments.statusLabel')}
            value={statuses}
            onChange={(value) =>
              update((next) => {
                next.delete('status')
                value.forEach((v) => next.append('status', v))
              })
            }
            options={STATUSES.map((s) => ({ value: s, label: t(`paymentStatus.${s}`) }))}
          />
        </FilterBar>

        <DataTable
          caption={t('pages.payments.title')}
          columns={columns}
          rows={query.data?.items}
          getRowId={(p) => p.id}
          loading={query.isPending}
          fetching={query.isFetching && !query.isPending}
          error={query.isError ? errorMessage(query.error) : undefined}
          onRetry={() => void query.refetch()}
          skeletonRows={8}
          emptyTitle={filtersActive ? t('payments.empty.filtered') : t('payments.empty.title')}
          emptyDescription={filtersActive ? t('payments.empty.filteredHint') : undefined}
          sort={sort}
          onSortChange={(next) =>
            update((p) => {
              p.set('sort', next.by)
              p.set('dir', next.dir)
            })
          }
          pagination={
            query.data
              ? {
                  page: query.data.page,
                  pageSize: query.data.pageSize,
                  totalItems: query.data.totalItems,
                  onPageChange: (value) => update((next) => next.set('page', String(value)), false),
                }
              : undefined
          }
          onRowClick={(p) => update((next) => next.set('id', p.id), false)}
        />
      </Card>

      <PaymentDrawer id={selectedId} onClose={() => update((next) => next.delete('id'), false)} />
    </>
  )
}
