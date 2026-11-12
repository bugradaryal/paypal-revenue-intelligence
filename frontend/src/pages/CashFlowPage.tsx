import { AlertTriangle, CalendarClock, CircleDollarSign, Coins, FileClock, FileText } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { EmptyState, MoneyText, StatCard } from '@/components/patterns'
import { Badge, Button, Card, CardHeader, Skeleton } from '@/components/ui'
import { env } from '@/config/env'
import { useCashFlowOutlook } from '@/features/revenue/queries'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { useI18n } from '@/i18n'
import { DAY_MS } from '@/lib/dates'

export function CashFlowPage() {
  const { t, tryT } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const outlook = useCashFlowOutlook()
  const data = outlook.data
  const date = (day: string) => f.date(`${day}T00:00:00Z`)
  const dayBefore = (day: string) => new Date(Date.parse(`${day}T00:00:00Z`) - DAY_MS).toISOString().slice(0, 10)
  const invoiceCount = (count: number) =>
    t(count === 1 ? 'cashFlow.stats.invoice' : 'cashFlow.stats.invoices', { count: f.number(count) })

  return (
    <>
      <PageHeader title={t('pages.cashFlow.title')} description={t('pages.cashFlow.description')} />

      {env.isMock && <p className="mb-5 text-xs text-ink-faint">{t('cashFlow.demoNotice')}</p>}

      {outlook.isPending ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          {[0, 1, 2, 3, 4].map((item) => <Skeleton key={item} className="h-32 w-full" />)}
        </div>
      ) : outlook.isError && !data ? (
        <EmptyState tone="danger" title={errorMessage(outlook.error)} action={
          <Button size="sm" variant="secondary" onClick={() => void outlook.refetch()}>{t('common.retry')}</Button>
        } />
      ) : data ? (
        <div className="grid gap-6">
          <section aria-label={t('cashFlow.summaryLabel')} className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
            <StatCard label={t('cashFlow.stats.open')} icon={<CircleDollarSign />} tone="brand"
              value={<MoneyText value={data.openReceivables} />} hint={invoiceCount(data.openInvoiceCount)} />
            <StatCard label={t('cashFlow.stats.overdue')} icon={<AlertTriangle />} tone="danger"
              value={<MoneyText value={data.overdueReceivables} />} hint={invoiceCount(data.overdueInvoiceCount)} />
            <StatCard label={t('cashFlow.stats.next7')} icon={<CalendarClock />} tone="warning"
              value={<MoneyText value={data.dueWithin7Days} />} hint={invoiceCount(data.dueWithin7DaysCount)} />
            <StatCard label={t('cashFlow.stats.next30')} icon={<FileClock />} tone="neutral"
              value={<MoneyText value={data.dueWithin30Days} />} hint={invoiceCount(data.dueWithin30DaysCount)} />
            <StatCard label={t('cashFlow.stats.next90')} icon={<Coins />} tone="neutral"
              value={<MoneyText value={data.dueWithin90Days} />} hint={invoiceCount(data.dueWithin90DaysCount)} />
          </section>

          {(data.overdueInvoiceCount > 0 || data.dueWithin7DaysCount > 0 || data.foreignCurrencyExposure.length > 0) && (
            <Card className="grid gap-4">
              <CardHeader className="mb-0" title={t('cashFlow.alerts.title')} description={t('cashFlow.alerts.description')} />
              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                {data.overdueInvoiceCount > 0 && (
                  <Link to="#receivables" className="focus-ring rounded-xl border border-danger/25 bg-danger-soft p-4 hover:border-danger/50">
                    <p className="font-semibold text-danger">{t('cashFlow.alerts.overdue', { count: f.number(data.overdueInvoiceCount) })}</p>
                    <MoneyText value={data.overdueReceivables} className="mt-2 text-lg font-bold" />
                  </Link>
                )}
                {data.dueWithin7DaysCount > 0 && (
                  <Link to="#receivables" className="focus-ring rounded-xl border border-warning/25 bg-warning-soft p-4 hover:border-warning/50">
                    <p className="font-semibold">{t('cashFlow.alerts.dueSoon', { count: f.number(data.dueWithin7DaysCount) })}</p>
                    <MoneyText value={data.dueWithin7Days} className="mt-2 text-lg font-bold" />
                  </Link>
                )}
                {data.foreignCurrencyExposure.length > 0 && (
                  <Link to="#currency-exposure" className="focus-ring rounded-xl border border-line bg-surface-muted p-4 hover:border-brand-300">
                    <p className="font-semibold">{t('cashFlow.alerts.foreignCurrency', { count: f.number(data.foreignCurrencyExposure.length) })}</p>
                    <p className="mt-2 text-sm text-ink-soft">{t('cashFlow.currency.noConversion')}</p>
                  </Link>
                )}
              </div>
            </Card>
          )}

          <div className="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(0,0.85fr)]">
            <Card className="grid content-start gap-5">
              <CardHeader
                className="mb-0"
                title={t('cashFlow.schedule.title')}
                description={t('cashFlow.schedule.description')}
                action={<Badge tone="neutral">{t('cashFlow.schedule.asOf', { date: date(data.asOfDateUtc) })}</Badge>}
              />
              <div className="grid gap-2">
                {data.weeklySchedule.map((week) => (
                  <div key={week.from} className="flex flex-wrap items-center justify-between gap-3 border-b border-line/70 py-3 last:border-0">
                    <div>
                      <p className="font-medium">{t('cashFlow.schedule.week', { from: date(week.from), to: date(dayBefore(week.toExclusive)) })}</p>
                      <p className="mt-0.5 text-xs text-ink-faint">{invoiceCount(week.invoiceCount)}</p>
                    </div>
                    <MoneyText value={week.amount} className="font-semibold" />
                  </div>
                ))}
              </div>
            </Card>

            <Card className="grid content-start gap-5">
              <CardHeader className="mb-0" title={t('cashFlow.aging.title')} description={t('cashFlow.aging.description')} />
              <div className="grid gap-3">
                {data.overdueAging.map((bucket) => (
                  <div key={bucket.key} className="flex items-center justify-between gap-3 rounded-lg bg-surface-muted p-3">
                    <div>
                      <p className="font-medium">{t(`cashFlow.aging.${bucket.key}`)}</p>
                      <p className="mt-0.5 text-xs text-ink-soft">{invoiceCount(bucket.invoiceCount)}</p>
                    </div>
                    <MoneyText value={bucket.amount} className="font-semibold" />
                  </div>
                ))}
                <div className="flex items-center justify-between gap-3 border-t border-line pt-3">
                  <span className="text-sm text-ink-soft">{t('cashFlow.unscheduled')}</span>
                  <MoneyText value={data.unscheduledReceivables} className="font-semibold" />
                </div>
              </div>
            </Card>
          </div>

          {data.foreignCurrencyExposure.length > 0 && (
            <Card id="currency-exposure" className="grid gap-4">
              <CardHeader className="mb-0" title={t('cashFlow.currency.title')} description={t('cashFlow.currency.description')} />
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.foreignCurrencyExposure.map((exposure) => (
                  <div key={exposure.currency} className="flex items-center justify-between gap-3 rounded-lg border border-line p-4">
                    <div>
                      <p className="font-semibold">{exposure.currency}</p>
                      <p className="mt-1 text-xs text-ink-soft">{invoiceCount(exposure.invoiceCount)}</p>
                    </div>
                    <MoneyText value={exposure.openAmount} className="font-semibold" />
                  </div>
                ))}
              </div>
              <p className="text-xs text-ink-faint">{t('cashFlow.currency.noConversion')}</p>
            </Card>
          )}

          <Card id="receivables" className="grid gap-5">
            <CardHeader className="mb-0" title={t('cashFlow.invoices.title')} description={t('cashFlow.invoices.description')} />
            {data.priorityInvoices.length ? (
              <div className="grid gap-3">
                {data.priorityInvoices.map((invoice) => (
                  <div key={invoice.id} className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-line p-4">
                    <div className="min-w-0">
                      <p className="flex flex-wrap items-center gap-2 font-semibold">
                        <FileText className="size-4 text-ink-faint" />
                        {invoice.customerName ?? t('cashFlow.invoices.unknownCustomer')}
                        {invoice.dataSource === 'demo' && <Badge tone="warning">{t('cashFlow.demoBadge')}</Badge>}
                      </p>
                      <p className="mt-1 text-xs text-ink-soft">
                        {invoice.invoiceNumber ?? invoice.id}
                        <span aria-hidden> · </span>
                        {invoice.dueDate ? date(invoice.dueDate) : t('cashFlow.invoice.noDueDate')}
                        {invoice.daysOverdue !== null && (
                          <span className="ml-2 font-semibold text-danger">
                            {t(invoice.daysOverdue === 1 ? 'cashFlow.invoice.oneDayOverdue' : 'cashFlow.invoice.daysOverdue', { count: f.number(invoice.daysOverdue) })}
                          </span>
                        )}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <Badge tone={invoice.daysOverdue !== null ? 'danger' : 'neutral'}>
                        {tryT(`invoiceStatus.${invoice.status}`, invoice.status)}
                      </Badge>
                      <MoneyText value={invoice.balanceDue} className="font-semibold" />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <EmptyState icon={<FileText />} title={t('cashFlow.invoices.empty')} description={t('cashFlow.invoices.emptyHint')} />
            )}
          </Card>

          <p className="text-xs text-ink-faint">{t('cashFlow.disclaimer')}</p>
        </div>
      ) : null}
    </>
  )
}
