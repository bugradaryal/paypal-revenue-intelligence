import { useMemo, useState } from 'react'
import { AlertTriangle, ArrowUpRight, CircleCheck, WandSparkles } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState, MoneyText, Pagination, StatCard } from '@/components/patterns'
import { Badge, Button, Card, CardHeader, Select, Skeleton, useToast } from '@/components/ui'
import { env } from '@/config/env'
import { SuggestionCard } from '@/features/recovery/SuggestionCard'
import { useSuggestionActions } from '@/features/recovery/useSuggestionActions'
import { useGenerateSuggestions, useIssues, useRecoverySummary, useSuggestions } from '@/features/revenue/queries'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { useI18n } from '@/i18n'
import { lastDays } from '@/lib/dates'
import type { RecoverySuggestionStatus, RevenueIssue } from '@/services/types'

type QueueFilter = 'all' | RecoverySuggestionStatus
type QueueSort = 'newest' | 'value'

const QUEUE_FILTERS: QueueFilter[] = ['all', 'proposed', 'approved', 'executed', 'failed', 'rejected']
const ISSUE_PREVIEW_SIZE = 6
const ISSUE_LIST_PAGE_SIZE = 20
const MAX_ISSUE_PAGE_SIZE = 100

function IssueRow({ issue }: { issue: RevenueIssue }) {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const href = issue.paymentId ? `/payments?id=${encodeURIComponent(issue.paymentId)}` : '/cash-flow'
  const reference = issue.paymentId
    ? t('commandCenter.issue.paymentReference', { id: issue.paymentId.slice(0, 8) })
    : t('commandCenter.issue.invoiceReference', { id: issue.invoiceId?.slice(0, 8) ?? '—' })

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-line p-4">
      <div className="min-w-0">
        <p className="font-semibold">{issue.customerName ?? t('commandCenter.issue.unknownCustomer')}</p>
        <p className="mt-1 flex flex-wrap items-center gap-2 text-xs text-ink-soft">
          <Link to={href} className="focus-ring rounded underline-offset-2 hover:text-ink hover:underline">
            {reference}
          </Link>
          <span aria-hidden>·</span>
          <span>{tryT(`issueType.${issue.type}`, issue.type)}</span>
          <span aria-hidden>·</span>
          <span>{t('commandCenter.issue.since', { date: f.date(issue.problemSince) })}</span>
        </p>
      </div>
      <div className="flex items-center gap-3">
        <Badge dot tone={issue.status === 'open' ? 'warning' : 'success'}>
          {tryT(`issueStatus.${issue.status}`, issue.status)}
        </Badge>
        <MoneyText value={issue.amount} className="font-semibold" />
      </div>
    </div>
  )
}

export function RecoveryCommandPage() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const [filter, setFilter] = useState<QueueFilter>('proposed')
  const [sort, setSort] = useState<QueueSort>('newest')
  const [queuePage, setQueuePage] = useState(1)
  const [issuesExpanded, setIssuesExpanded] = useState(false)
  const [issuePageSize, setIssuePageSize] = useState(ISSUE_LIST_PAGE_SIZE)
  const range = useMemo(() => lastDays(30), [])
  const summary = useRecoverySummary(range)
  const openIssues = useIssues({ page: 1, pageSize: issuePageSize, status: ['open'], sortBy: 'amount', sortDir: 'desc' })
  const allIssues = useIssues({ page: 1, pageSize: 100, sortBy: 'problemSince', sortDir: 'desc' })
  const suggestions = useSuggestions({
    page: queuePage,
    pageSize: 8,
    status: filter === 'all' ? undefined : [filter],
    sortBy: sort === 'value' ? 'expectedRecovery' : 'createdAt',
    sortDir: 'desc',
  })
  const actions = useSuggestionActions()
  const generate = useGenerateSuggestions()
  const issueById = new Map((allIssues.data?.items ?? []).map((issue) => [issue.id, issue]))

  function onGenerate() {
    generate.mutate(undefined, {
      onSuccess: ({ createdCount }) =>
        toast({
          tone: createdCount > 0 ? 'success' : 'info',
          title: createdCount > 0
            ? t('recovery.toast.generated', { count: f.number(createdCount) })
            : t('recovery.toast.generatedNone'),
        }),
      onError: (error) => toast({ tone: 'danger', title: errorMessage(error) }),
    })
  }

  const s = summary.data
  const issues = openIssues.data?.items
  const visibleIssues = issuesExpanded ? issues : issues?.slice(0, ISSUE_PREVIEW_SIZE)
  const hasMoreIssuePages = (openIssues.data?.totalItems ?? 0) > (issues?.length ?? 0) && issuePageSize < MAX_ISSUE_PAGE_SIZE
  const actionItems = suggestions.data?.items

  return (
    <>
      <PageHeader
        title={t('pages.commandCenter.title')}
        description={t('pages.commandCenter.description')}
        actions={
          <Button leadingIcon={<WandSparkles className="size-4" />} loading={generate.isPending} onClick={onGenerate}>
            {generate.isPending ? t('recovery.generating') : t('commandCenter.generate')}
          </Button>
        }
      />

      {env.isMock && <p className="mb-5 text-xs text-ink-faint">{t('commandCenter.demoNotice')}</p>}

      <section aria-label={t('commandCenter.summaryLabel')} className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label={t('recovery.stats.lost')} icon={<AlertTriangle />} tone="danger" loading={summary.isPending}
          error={summary.isError ? errorMessage(summary.error) : undefined} onRetry={() => void summary.refetch()}
          value={s && <MoneyText value={s.lostAmount} />} hint={t('recovery.stats.lostHint')} />
        <StatCard label={t('recovery.stats.recovered')} icon={<CircleCheck />} tone="success" loading={summary.isPending}
          error={summary.isError ? errorMessage(summary.error) : undefined} onRetry={() => void summary.refetch()}
          value={s && <MoneyText value={s.recoveredAmount} />} hint={t('commandCenter.summary.recoveredHint')} />
        <StatCard label={t('commandCenter.summary.openIssues')} icon={<AlertTriangle />} tone="warning" loading={openIssues.isPending}
          error={openIssues.isError ? errorMessage(openIssues.error) : undefined} onRetry={() => void openIssues.refetch()}
          value={openIssues.data && <span className="tabular">{f.number(openIssues.data.totalItems)}</span>}
          hint={t('commandCenter.summary.openIssuesHint')} />
        <StatCard label={t('recovery.stats.open')} icon={<ArrowUpRight />} tone="brand" loading={summary.isPending}
          error={summary.isError ? errorMessage(summary.error) : undefined} onRetry={() => void summary.refetch()}
          value={s && <span className="tabular">{f.number(s.openSuggestionCount)}</span>} hint={t('recovery.stats.openHint')} />
      </section>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)]">
        <Card className={`flex min-h-0 flex-col gap-5 ${issuesExpanded ? 'self-stretch' : ''}`}>
          <CardHeader
            className="mb-0"
            title={t('commandCenter.issues.title')}
            description={t('commandCenter.issues.description')}
            action={
              <div className="flex items-center gap-2">
                <Badge tone="warning">{f.number(openIssues.data?.totalItems ?? 0)}</Badge>
                {issues && (issues.length > ISSUE_PREVIEW_SIZE || (issuesExpanded && hasMoreIssuePages)) && (
                  <Button
                    size="sm"
                    variant="ghost"
                    disabled={openIssues.isFetching}
                    onClick={() => {
                      if (!issuesExpanded) setIssuesExpanded(true)
                      else if (hasMoreIssuePages) {
                        setIssuePageSize((size) => Math.min(size + ISSUE_LIST_PAGE_SIZE, MAX_ISSUE_PAGE_SIZE))
                      } else setIssuesExpanded(false)
                    }}
                  >
                    {issuesExpanded && hasMoreIssuePages
                      ? t('commandCenter.issues.viewMore', {
                        count: f.number(Math.min(ISSUE_LIST_PAGE_SIZE, (openIssues.data?.totalItems ?? 0) - issues.length)),
                      })
                      : issuesExpanded
                        ? t('commandCenter.issues.viewLess')
                        : t('commandCenter.issues.viewMore', { count: f.number(issues.length - ISSUE_PREVIEW_SIZE) })}
                  </Button>
                )}
              </div>
            }
          />
          {openIssues.isPending ? (
            <div className="grid gap-3">{[0, 1, 2].map((item) => <Skeleton key={item} className="h-20 w-full" />)}</div>
          ) : openIssues.isError ? (
            <EmptyState tone="danger" title={errorMessage(openIssues.error)} action={
              <Button size="sm" variant="secondary" onClick={() => void openIssues.refetch()}>{t('common.retry')}</Button>
            } />
          ) : issues?.length ? (
            <>
              <div
                role="region"
                aria-label={t('commandCenter.issues.title')}
                tabIndex={0}
                className={`grid gap-3 ${issuesExpanded ? 'max-h-[60rem] min-h-0 flex-1 overflow-y-auto overscroll-contain pr-2' : ''}`}
              >
                {visibleIssues?.map((issue) => <IssueRow key={issue.id} issue={issue} />)}
              </div>
              {issuesExpanded && openIssues.data.totalItems > issues.length && (
                <p className="text-xs text-ink-faint">{t('commandCenter.issues.more', { count: f.number(openIssues.data.totalItems - issues.length) })}</p>
              )}
            </>
          ) : (
            <EmptyState icon={<CircleCheck />} title={t('commandCenter.issues.empty')} description={t('commandCenter.issues.emptyHint')} />
          )}
        </Card>

        <Card className="grid content-start gap-5">
          <CardHeader
            className="mb-0"
            title={t('commandCenter.queue.title')}
            description={t('commandCenter.queue.description')}
            action={
              <div className="flex flex-wrap justify-end gap-2">
                <div className="w-44">
                  <Select
                    aria-label={t('commandCenter.queue.filterLabel')}
                    value={filter}
                    onChange={(event) => {
                      setFilter(event.target.value as QueueFilter)
                      setQueuePage(1)
                    }}
                    options={QUEUE_FILTERS.map((status) => ({
                      value: status,
                      label: status === 'all' ? t('commandCenter.queue.all') : t(`suggestionStatus.${status}`),
                    }))}
                  />
                </div>
                <div className="w-40">
                  <Select
                    aria-label={t('recovery.sortLabel')}
                    value={sort}
                    onChange={(event) => {
                      setSort(event.target.value as QueueSort)
                      setQueuePage(1)
                    }}
                    options={[
                      { value: 'newest', label: t('recovery.sort.newest') },
                      { value: 'value', label: t('recovery.sort.value') },
                    ]}
                  />
                </div>
              </div>
            }
          />
          {generate.isPending && <p className="-mt-2 text-xs text-ink-soft">{t('commandCenter.generatingHint')}</p>}
          {suggestions.isPending ? (
            <div className="grid gap-3">{[0, 1].map((item) => <Skeleton key={item} className="h-40 w-full" />)}</div>
          ) : suggestions.isError ? (
            <EmptyState tone="danger" title={errorMessage(suggestions.error)} action={
              <Button size="sm" variant="secondary" onClick={() => void suggestions.refetch()}>{t('common.retry')}</Button>
            } />
          ) : actionItems?.length ? (
            <div role="region" aria-label={t('commandCenter.queue.title')} tabIndex={0} className="max-h-[60rem] overflow-y-auto overscroll-contain pr-2">
              <div className="grid gap-4">
                {actionItems.map((suggestion) => {
                  const issue = issueById.get(suggestion.issueId)
                  return (
                    <div key={suggestion.id} className="grid gap-2">
                      <SuggestionCard
                        suggestion={suggestion}
                        deciding={actions.deciding(suggestion.id)}
                        busy={actions.busy}
                        onApprove={actions.onApprove}
                        onReject={actions.onReject}
                      />
                      {issue?.status === 'recovered' && issue.evidence && (
                        <div className="flex flex-wrap items-center gap-2 px-2 text-xs text-success">
                          <span>{t('commandCenter.queue.verifiedRecovery', { evidence: t(`recoveryEvidence.${issue.evidence}`) })}</span>
                          <MoneyText value={issue.recoveredAmount} />
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          ) : (
            <EmptyState
              icon={<CircleCheck />}
              title={t('commandCenter.queue.empty')}
              description={t('commandCenter.queue.emptyHint')}
              action={<Link to="/dashboard" className="focus-ring rounded text-sm font-semibold text-brand-700 hover:underline">{t('commandCenter.queue.openOverview')}</Link>}
            />
          )}
          {suggestions.data && (
            <Pagination
              page={suggestions.data.page}
              pageSize={suggestions.data.pageSize}
              totalItems={suggestions.data.totalItems}
              onPageChange={setQueuePage}
            />
          )}
        </Card>
      </div>

      {actions.confirmDialog}
    </>
  )
}
