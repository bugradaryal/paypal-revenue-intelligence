import { useEffect, useState, type FormEvent } from 'react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Badge, Button, Card, CardHeader, Dialog, Field, Input, Select, Skeleton, Switch, useToast } from '@/components/ui'
import { DataTable, EmptyState, MoneyText, type Column } from '@/components/patterns'
import { useCacheStats, useBudgetSetting, useClearCache, useModelCatalog, useUpdateBudget } from '@/features/settings/queries'
import { useTheme } from '@/app/theme'
import { LOCALES, useI18n, type Locale } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { useFormat } from '@/i18n/useFormat'
import { ApiError } from '@/services/errors'
import type { AiModelInfo } from '@/services/types'

/** Only what the backend can store is editable here. Language and theme live in this browser. */

// A decimal with a "." separator and at most two digits, the format the contract uses for money.
const AMOUNT = /^\d{1,12}(\.\d{1,2})?$/

function BudgetCard() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const budget = useBudgetSetting()
  const update = useUpdateBudget()
  const [amount, setAmount] = useState('')
  const [touched, setTouched] = useState(false)

  const saved = budget.data?.limit.amount
  const currency = budget.data?.limit.currency ?? 'USD'
  useEffect(() => {
    if (budget.data !== undefined) setAmount(budget.data?.limit.amount ?? '')
  }, [budget.data])

  const valid = AMOUNT.test(amount) && Number(amount) > 0
  const changed = amount !== (saved ?? '')
  const serverRejected = update.error instanceof ApiError && update.error.code === 'validation_failed'

  function onSubmit(event: FormEvent) {
    event.preventDefault()
    setTouched(true)
    if (!valid || !changed) return
    update.mutate(
      { limit: { amount, currency } },
      {
        onSuccess: () => toast({ tone: 'success', title: t('settings.budget.saved') }),
      },
    )
  }

  return (
    <Card>
      <CardHeader title={t('settings.budget.title')} description={t('settings.budget.description')} />
      {budget.isPending ? (
        <Skeleton className="h-10 w-full max-w-xs" />
      ) : budget.isError ? (
        <EmptyState tone="danger" title={errorMessage(budget.error)} className="py-4" action={<Button size="sm" variant="secondary" onClick={() => void budget.refetch()}>{t('common.retry')}</Button>} />
      ) : (
        <form onSubmit={onSubmit} className="grid max-w-sm gap-4" noValidate>
          <Field
            label={t('settings.budget.limit', { currency })}
            hint={budget.data ? undefined : t('settings.budget.none')}
            error={(touched && !valid ? t('settings.budget.invalid') : undefined) ?? (serverRejected ? errorMessage(update.error) : undefined)}
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                inputMode="decimal"
                autoComplete="off"
                value={amount}
                invalid={invalid}
                aria-describedby={describedBy}
                onChange={(e) => {
                  setAmount(e.target.value.trim())
                  update.reset()
                }}
              />
            )}
          </Field>
          {update.isError && !serverRejected && <p role="alert" className="text-sm text-danger">{errorMessage(update.error)}</p>}
          <div>
            <Button type="submit" loading={update.isPending} disabled={!changed}>
              {t('common.save')}
            </Button>
          </div>
        </form>
      )}
    </Card>
  )
}

function ModelsCard() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const models = useModelCatalog()

  const columns: Column<AiModelInfo>[] = [
    {
      key: 'model',
      header: t('settings.models.model'),
      cell: (m) => (
        <span className="flex flex-wrap items-center gap-2 font-medium">
          {m.label}
          {m.baseline && (
            <Badge size="sm" tone="brand">
              {t('settings.models.baseline')}
            </Badge>
          )}
        </span>
      ),
    },
    { key: 'provider', header: t('settings.models.provider'), hideBelow: 'sm', cell: (m) => m.provider },
    { key: 'in', header: t('settings.models.input'), numeric: true, hideBelow: 'md', cell: (m) => <MoneyText value={m.inputPricePerMillionTokens} /> },
    { key: 'out', header: t('settings.models.output'), numeric: true, cell: (m) => <MoneyText value={m.outputPricePerMillionTokens} /> },
  ]

  return (
    <Card>
      <CardHeader title={t('settings.models.title')} description={t('settings.models.description')} />
      <DataTable
        caption={t('settings.models.title')}
        columns={columns}
        rows={models.data}
        getRowId={(m) => m.key}
        loading={models.isPending}
        error={models.isError ? errorMessage(models.error) : undefined}
        onRetry={() => void models.refetch()}
        skeletonRows={3}
        emptyTitle={t('settings.models.empty')}
      />
    </Card>
  )
}

function CacheCard() {
  const { t } = useI18n()
  const f = useFormat()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const stats = useCacheStats()
  const clear = useClearCache()
  const [confirming, setConfirming] = useState(false)

  return (
    <Card>
      <CardHeader title={t('settings.cache.title')} description={t('settings.cache.description')} />
      {stats.isPending ? (
        <Skeleton className="h-10 w-48" />
      ) : stats.isError ? (
        <EmptyState tone="danger" title={errorMessage(stats.error)} className="py-4" action={<Button size="sm" variant="secondary" onClick={() => void stats.refetch()}>{t('common.retry')}</Button>} />
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-4">
          <p className="text-sm">
            <span className="tabular text-2xl font-bold">{f.number(stats.data.entryCount)}</span>{' '}
            <span className="text-ink-soft">{t('settings.cache.entries')}</span>
          </p>
          <Button variant="secondary" disabled={stats.data.entryCount === 0} onClick={() => setConfirming(true)}>
            {t('settings.cache.clear')}
          </Button>
        </div>
      )}

      <Dialog
        open={confirming}
        onClose={() => setConfirming(false)}
        title={t('settings.cache.confirmTitle')}
        description={t('settings.cache.confirmBody')}
        footer={
          <>
            <Button variant="ghost" onClick={() => setConfirming(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              loading={clear.isPending}
              onClick={() =>
                clear.mutate(undefined, {
                  onSuccess: ({ removedCount }) => {
                    setConfirming(false)
                    toast({ tone: 'success', title: t('settings.cache.cleared', { count: f.number(removedCount) }) })
                  },
                  onError: (error) => {
                    setConfirming(false)
                    toast({ tone: 'danger', title: errorMessage(error) })
                  },
                })
              }
            >
              {t('settings.cache.clear')}
            </Button>
          </>
        }
      >
        <span />
      </Dialog>
    </Card>
  )
}

export function SettingsPage() {
  const { t, locale, setLocale } = useI18n()
  const { theme, setTheme } = useTheme()

  return (
    <>
      <PageHeader title={t('pages.settings.title')} description={t('pages.settings.description')} />

      <div className="grid grid-cols-[minmax(0,1fr)] gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader title={t('settings.preferences.title')} description={t('settings.preferences.description')} />
          <div className="grid max-w-sm gap-5">
            <Field label={t('settings.preferences.language')}>
              {({ id }) => (
                <Select id={id} value={locale} onChange={(e) => setLocale(e.target.value as Locale)} options={LOCALES.map((l) => ({ value: l.code, label: l.label }))} />
              )}
            </Field>
            <Switch
              checked={theme === 'dark'}
              onCheckedChange={(on) => setTheme(on ? 'dark' : 'light')}
              label={t('settings.preferences.dark')}
              description={t('settings.preferences.darkHint')}
            />
          </div>
        </Card>

        <BudgetCard />
        <CacheCard />
        <ModelsCard />
      </div>
    </>
  )
}
