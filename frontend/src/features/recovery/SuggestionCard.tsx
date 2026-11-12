import { Link } from 'react-router'
import { Info, Send } from 'lucide-react'
import { Badge, Button, Card, Spinner } from '@/components/ui'
import { MoneyText, StatusPill } from '@/components/patterns'
import { LOCALES, useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'
import type { RecoverySuggestion } from '@/services/types'

interface SuggestionCardProps {
  suggestion: RecoverySuggestion
  /** true while this suggestion's decision request is running. */
  deciding: boolean
  /** true while another decision is running; buttons wait. */
  busy: boolean
  onApprove: (s: RecoverySuggestion) => void
  onReject: (s: RecoverySuggestion) => void
}

/** One AI-proposed action. The user's approval is the only way it can run. */
export function SuggestionCard({ suggestion: s, deciding, busy, onApprove, onReject }: SuggestionCardProps) {
  const { t, tryT, locale } = useI18n()
  const f = useFormat()
  const writtenIn = LOCALES.find((l) => l.code === s.rationaleLanguage)?.label ?? s.rationaleLanguage
  const running = s.status === 'approved' && s.executable
  const displayStatus = s.status === 'approved' && !s.executable ? 'reviewed' : s.status
  const recordHref = s.invoiceId ? '/cash-flow' : s.paymentId ? `/payments?id=${encodeURIComponent(s.paymentId)}` : undefined
  const recordLabel = s.invoiceId
    ? t('recovery.card.invoiceReference', { id: s.invoiceId.slice(0, 8) })
    : s.paymentId ?? t('recovery.card.noLinkedRecord')

  return (
    <Card padding="sm" className="grid min-w-0 gap-4 p-5">
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
        <div className="min-w-0">
          <p className="flex flex-wrap items-center gap-2 font-semibold">
            {s.customerName ?? t('recovery.card.unknownCustomer')}
          </p>
          <p className="mt-0.5 text-xs text-ink-faint">
            {recordHref ? (
              <Link to={recordHref} className="tabular focus-ring rounded underline-offset-2 hover:text-ink hover:underline">
                {recordLabel}
              </Link>
            ) : (
              <span>{recordLabel}</span>
            )}
            <span aria-hidden> · </span>
            {f.date(s.createdAt)}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone="brand">{tryT(`suggestionType.${s.type}`, s.type)}</Badge>
          {s.dataSource === 'demo' && <Badge tone="warning">{t('recovery.card.demo')}</Badge>}
          <StatusPill kind="suggestion" status={displayStatus} />
        </div>
      </div>

      <div className="grid gap-1.5">
        {/* Written by the AI on the backend; shown as received. */}
        <p className="text-sm">{s.rationale}</p>
        {s.status === 'failed' && s.executionError && (
          <p role="alert" className="rounded-md bg-danger-soft px-3 py-2 text-xs text-danger">
            {t('recovery.card.executionFailed', { error: s.executionError })}
          </p>
        )}
        {s.rationaleLanguage !== locale && (
          <p className="text-xs text-ink-faint">{t('recovery.card.writtenIn', { language: writtenIn })}</p>
        )}
      </div>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs text-ink-soft">{t('recovery.card.expected')}</p>
          <MoneyText value={s.expectedRecovery} className="text-lg font-bold" />
        </div>

        {s.status === 'proposed' ? (
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" disabled={busy} onClick={() => onReject(s)}>
              {t('common.reject')}
            </Button>
            <Button size="sm" loading={deciding} disabled={busy && !deciding} onClick={() => onApprove(s)}>
              {s.executable ? t('common.approve') : t('recovery.card.markReviewed')}
            </Button>
          </div>
        ) : running ? (
          <p role="status" className="flex items-center gap-2 text-xs text-ink-soft">
            <Spinner className="size-4" />
            {t('recovery.card.running')}
          </p>
        ) : null}
      </div>

      <p className="flex items-start gap-2 border-t border-line pt-3 text-xs text-ink-soft">
        {s.executable ? <Send className="mt-0.5 size-3.5 shrink-0" /> : <Info className="mt-0.5 size-3.5 shrink-0" />}
        {s.executable ? t('recovery.card.executable') : t('recovery.card.adviceOnly')}
      </p>
    </Card>
  )
}
