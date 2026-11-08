import { Badge } from '@/components/ui'
import { MoneyText } from '@/components/patterns'
import { useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'
import type { ChatUsage } from '@/services/types'

/**
 * What this answer cost: model, tokens and money, straight from the backend's usage event.
 * Keeping the price of every answer in view is what ties the Workspace to the AI Cost page.
 */
export function Receipt({ usage }: { usage: ChatUsage }) {
  const { t } = useI18n()
  const f = useFormat()

  return (
    <dl
      aria-label={t('workspace.receipt.label')}
      className="tabular flex flex-wrap items-center gap-x-3 gap-y-1 rounded-md border border-dashed border-line px-3 py-1.5 text-[11px] text-ink-soft"
    >
      <div className="flex gap-1">
        <dt className="sr-only">{t('workspace.receipt.model')}</dt>
        <dd className="font-medium text-ink">{usage.model}</dd>
        <dd>· {usage.provider}</dd>
      </div>
      <div className="flex gap-1">
        <dt>{t('workspace.receipt.tokens')}</dt>
        <dd>
          {f.number(usage.inputTokens)} / {f.number(usage.outputTokens)}
        </dd>
      </div>
      <div className="flex gap-1">
        <dt>{t('workspace.receipt.cost')}</dt>
        <dd className="font-medium text-ink">
          <MoneyText value={usage.cost} maximumFractionDigits={6} />
        </dd>
      </div>
      {usage.cacheHit && (
        <Badge size="sm" tone="success">
          {t('workspace.receipt.cacheHit')}
        </Badge>
      )}
    </dl>
  )
}
