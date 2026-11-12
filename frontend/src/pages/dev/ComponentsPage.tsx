import { useState, type ReactNode } from 'react'
import { Download, MoreHorizontal, Pencil, Search, Trash2 } from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardHeader,
  Checkbox,
  Dialog,
  Dropdown,
  Field,
  IconButton,
  Input,
  Select,
  Skeleton,
  Spinner,
  Switch,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
  Tabs,
  TabsList,
  TabsPanel,
  Textarea,
  Tooltip,
  useToast,
} from '@/components/ui'
import { useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'
import type { PaymentStatus } from '@/services/types'
import { DevHeader } from './DevHeader'

// Sample data used only to show components. Names and ids are fictional.
const SAMPLE_PAYMENT_ID = 'PAY-1043'
const SAMPLE_CUSTOMER = 'Nova Studio'
const rows: Array<{ id: string; customer: string; amount: string; status: Extract<PaymentStatus, 'completed' | 'failed' | 'pending'> }> = [
  { id: 'PAY-1042', customer: 'Acme GmbH', amount: '1240.00', status: 'completed' },
  { id: SAMPLE_PAYMENT_ID, customer: SAMPLE_CUSTOMER, amount: '389.90', status: 'failed' },
  { id: 'PAY-1044', customer: 'Lumen Labs', amount: '72.50', status: 'pending' },
]
const statusTone = { completed: 'success', failed: 'danger', pending: 'warning' } as const

function Block({ title, hint, children }: { title: string; hint?: string; children: ReactNode }) {
  return (
    <Card>
      <CardHeader title={title} description={hint} />
      {children}
    </Card>
  )
}

export function ComponentsPage() {
  const { t } = useI18n()
  const f = useFormat()
  const { toast } = useToast()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [notify, setNotify] = useState(true)
  const [loading, setLoading] = useState(false)

  function fakeSave() {
    setLoading(true)
    window.setTimeout(() => {
      setLoading(false)
      toast({ title: t('dev.ds.toastSaved'), description: t('dev.ds.toastSavedBody'), tone: 'success' })
    }, 1200)
  }

  return (
    <div className="min-h-dvh bg-surface-app">
      <DevHeader current="components" />

      <main className="mx-auto grid max-w-6xl grid-cols-[minmax(0,1fr)] gap-6 px-4 py-10 sm:px-6">
        <div className="animate-fade-in">
          <p className="tabular text-xs font-medium uppercase tracking-widest text-brand-text">{t('dev.ds.eyebrow')}</p>
          <h1 className="mt-2 text-3xl font-bold tracking-tight sm:text-4xl">{t('dev.ds.title')}</h1>
          <p className="mt-3 max-w-xl text-ink-soft">{t('dev.ds.intro')}</p>
        </div>

        <Block title={t('dev.ds.buttonTitle')} hint={t('dev.ds.buttonHint')}>
          <div className="grid gap-5">
            <div className="flex flex-wrap items-center gap-3">
              <Button>{t('dev.ds.primary')}</Button>
              <Button variant="secondary">{t('dev.ds.secondary')}</Button>
              <Button variant="soft">{t('dev.ds.soft')}</Button>
              <Button variant="ghost">{t('dev.ds.ghost')}</Button>
              <Button variant="danger">{t('dev.ds.danger')}</Button>
              <Button disabled>{t('dev.ds.disabled')}</Button>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <Button size="sm">{t('dev.ds.small')}</Button>
              <Button size="md">{t('dev.ds.medium')}</Button>
              <Button size="lg">{t('dev.ds.large')}</Button>
              <Button leadingIcon={<Download className="size-4" />} variant="secondary">
                {t('common.export')}
              </Button>
              <Button loading={loading} onClick={fakeSave}>
                {t('common.save')}
              </Button>
              <Tooltip content={t('common.edit')}>
                <IconButton aria-label={t('common.edit')} variant="secondary">
                  <Pencil />
                </IconButton>
              </Tooltip>
              <IconButton aria-label={t('common.delete')} variant="soft">
                <Trash2 />
              </IconButton>
            </div>
          </div>
        </Block>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <Block title={t('dev.ds.formsTitle')} hint={t('dev.ds.formsHint')}>
            <div className="grid gap-4">
              <Field label={t('dev.ds.searchLabel')} hint={t('dev.ds.searchHint')}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    leadingIcon={<Search />}
                    placeholder={t('dev.ds.searchPlaceholder')}
                  />
                )}
              </Field>
              <Field label={t('common.amount')} required error={t('dev.ds.amountError')}>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} defaultValue="0" className="tabular" />
                )}
              </Field>
              <Field label={t('dev.ds.providerLabel')}>
                {({ id }) => (
                  <Select
                    id={id}
                    defaultValue="openai"
                    options={[
                      { value: 'openai', label: 'OpenAI' },
                      { value: 'google', label: 'Google' },
                    ]}
                  />
                )}
              </Field>
              <Field label={t('dev.ds.noteLabel')}>
                {({ id }) => <Textarea id={id} rows={3} placeholder={t('dev.ds.notePlaceholder')} />}
              </Field>
              <Checkbox label={t('dev.ds.weeklyLabel')} description={t('dev.ds.weeklyHint')} defaultChecked />
              <Switch
                checked={notify}
                onCheckedChange={setNotify}
                label={t('dev.ds.alertsLabel')}
                description={t('dev.ds.alertsHint')}
              />
            </div>
          </Block>

          <div className="grid min-w-0 content-start gap-6">
            <Block title={t('dev.ds.badgeTitle')} hint={t('dev.ds.badgeHint')}>
              <div className="flex flex-wrap gap-2">
                <Badge>{t('dev.ds.neutral')}</Badge>
                <Badge tone="brand">{t('dev.ds.brand')}</Badge>
                <Badge tone="success" dot>
                  {t('paymentStatus.completed')}
                </Badge>
                <Badge tone="warning" dot>
                  {t('paymentStatus.pending')}
                </Badge>
                <Badge tone="danger" dot>
                  {t('paymentStatus.failed')}
                </Badge>
                <Badge tone="info">{t('dev.ds.info')}</Badge>
                <Badge size="sm" tone="brand">
                  {t('dev.ds.small')}
                </Badge>
              </div>
            </Block>

            <Block title={t('dev.ds.tabsTitle')} hint={t('dev.ds.tabsHint')}>
              <Tabs defaultValue="all">
                <TabsList label={t('dev.ds.tabsLabel')}>
                  <Tab value="all">{t('common.all')}</Tab>
                  <Tab value="failed">{t('paymentStatus.failed')}</Tab>
                  <Tab value="pending">{t('paymentStatus.pending')}</Tab>
                </TabsList>
                <TabsPanel value="all" className="mt-4 text-sm text-ink-soft">
                  {t('dev.ds.tabAllText')}
                </TabsPanel>
                <TabsPanel value="failed" className="mt-4 text-sm text-ink-soft">
                  {t('dev.ds.tabFailedText')}
                </TabsPanel>
                <TabsPanel value="pending" className="mt-4 text-sm text-ink-soft">
                  {t('dev.ds.tabPendingText')}
                </TabsPanel>
              </Tabs>
            </Block>

            <Block title={t('dev.ds.loadingTitle')} hint={t('dev.ds.loadingHint')}>
              <div className="grid gap-3">
                <div className="flex items-center gap-3 text-ink-soft">
                  <Spinner className="size-5 text-brand-500" /> {t('dev.ds.fetching')}
                </div>
                <Skeleton className="h-4 w-2/3" />
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-24 w-full rounded-lg" />
              </div>
            </Block>
          </div>
        </div>

        <Block title={t('dev.ds.tableTitle')} hint={t('dev.ds.tableHint')}>
          <Table>
            <TableHead>
              <tr>
                <TableHeaderCell>{t('common.payment')}</TableHeaderCell>
                <TableHeaderCell>{t('common.customer')}</TableHeaderCell>
                <TableHeaderCell>{t('common.status')}</TableHeaderCell>
                <TableHeaderCell numeric>{t('common.amount')}</TableHeaderCell>
                <TableHeaderCell className="w-12">
                  <span className="sr-only">{t('common.actions')}</span>
                </TableHeaderCell>
              </tr>
            </TableHead>
            <TableBody>
              {rows.map((r) => (
                <TableRow key={r.id}>
                  <TableCell className="tabular text-xs">{r.id}</TableCell>
                  <TableCell className="font-medium">{r.customer}</TableCell>
                  <TableCell>
                    <Badge tone={statusTone[r.status]} dot>
                      {t(`paymentStatus.${r.status}`)}
                    </Badge>
                  </TableCell>
                  <TableCell numeric>{f.money({ amount: r.amount, currency: 'USD' })}</TableCell>
                  <TableCell>
                    <Dropdown
                      trigger={(props) => (
                        <IconButton aria-label={t('dev.ds.actionsFor', { id: r.id })} size="sm" {...props}>
                          <MoreHorizontal />
                        </IconButton>
                      )}
                      items={[
                        { id: 'view', label: t('dev.ds.viewDetails'), icon: <Search />, onSelect: () => setDrawerOpen(true) },
                        {
                          id: 'retry',
                          label: t('dev.ds.retryPayment'),
                          icon: <Pencil />,
                          onSelect: () => toast({ title: t('dev.ds.toastRetryQueued', { id: r.id }), tone: 'info' }),
                        },
                        {
                          id: 'del',
                          label: t('common.remove'),
                          icon: <Trash2 />,
                          danger: true,
                          onSelect: () => toast({ title: t('dev.ds.toastUnavailable'), tone: 'warning' }),
                        },
                      ]}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Block>

        <Block title={t('dev.ds.cardsTitle')}>
          <div className="grid gap-4 md:grid-cols-4">
            <Card padding="sm">{t('dev.ds.cardDefault')}</Card>
            <Card variant="muted" padding="sm">
              {t('dev.ds.cardMuted')}
            </Card>
            <Card variant="brand" padding="sm">
              {t('dev.ds.cardBrand')}
            </Card>
            <Card interactive padding="sm">
              {t('dev.ds.cardInteractive')}
            </Card>
          </div>
          <div className="mt-5 flex flex-wrap gap-3">
            <Button variant="secondary" onClick={() => setDialogOpen(true)}>
              {t('dev.ds.openDialog')}
            </Button>
            <Button variant="secondary" onClick={() => setDrawerOpen(true)}>
              {t('dev.ds.openDrawer')}
            </Button>
            <Button
              variant="soft"
              onClick={() =>
                toast({
                  title: t('dev.ds.toastRecovered'),
                  description: t('dev.ds.toastRecoveredBody', { id: SAMPLE_PAYMENT_ID }),
                  tone: 'success',
                })
              }
            >
              {t('dev.ds.successToast')}
            </Button>
            <Button variant="soft" onClick={() => toast({ title: t('dev.ds.toastLimit'), tone: 'warning' })}>
              {t('dev.ds.warningToast')}
            </Button>
            <Button
              variant="soft"
              onClick={() => toast({ title: t('dev.ds.toastFailed'), description: t('dev.ds.toastFailedBody'), tone: 'danger' })}
            >
              {t('dev.ds.errorToast')}
            </Button>
          </div>
        </Block>
      </main>

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={t('dev.ds.dialogTitle')}
        description={t('dev.ds.dialogDescription')}
        footer={
          <>
            <Button variant="ghost" onClick={() => setDialogOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              onClick={() => {
                setDialogOpen(false)
                toast({ title: t('dev.ds.toastApproved'), tone: 'success' })
              }}
            >
              {t('common.approve')}
            </Button>
          </>
        }
      >
        <p className="text-sm text-ink-soft">{t('dev.ds.dialogBody')}</p>
      </Dialog>

      <Dialog
        variant="drawer"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        title={SAMPLE_PAYMENT_ID}
        description={t('dev.ds.drawerDescription')}
      >
        <dl className="grid gap-4 text-sm">
          <div className="flex justify-between">
            <dt className="text-ink-soft">{t('common.customer')}</dt>
            <dd className="font-medium">{SAMPLE_CUSTOMER}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-ink-soft">{t('common.amount')}</dt>
            <dd className="tabular">{f.money({ amount: '389.90', currency: 'USD' })}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-ink-soft">{t('common.status')}</dt>
            <dd>
              <Badge tone="danger" dot>
                {t('paymentStatus.failed')}
              </Badge>
            </dd>
          </div>
        </dl>
      </Dialog>
    </div>
  )
}
