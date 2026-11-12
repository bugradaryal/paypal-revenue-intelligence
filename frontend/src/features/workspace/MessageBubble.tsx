import { Copy, RefreshCw, Sparkles } from 'lucide-react'
import { Badge, IconButton, Tooltip, useToast } from '@/components/ui'
import { SuggestionCard } from '@/features/recovery/SuggestionCard'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'
import type { RecoverySuggestion } from '@/services/types'
import { Receipt } from './Receipt'
import type { ChatTurn } from './useWorkspaceChat'

interface MessageBubbleProps {
  turn: ChatTurn
  isLast: boolean
  disabled: boolean
  onRegenerate: () => void
  actions: {
    onApprove: (s: RecoverySuggestion) => void
    onReject: (s: RecoverySuggestion) => void
    deciding: (id: string) => boolean
    busy: boolean
  }
}

export function MessageBubble({ turn, isLast, disabled, onRegenerate, actions }: MessageBubbleProps) {
  const { t } = useI18n()
  const { toast } = useToast()

  if (turn.role === 'user') {
    return (
      <div className="flex justify-end">
        <p className="max-w-[85%] whitespace-pre-wrap rounded-2xl rounded-br-md bg-brand-soft px-4 py-2.5 text-sm [overflow-wrap:anywhere]">
          {turn.content}
        </p>
      </div>
    )
  }

  async function copy() {
    try {
      await navigator.clipboard.writeText(turn.content)
      toast({ tone: 'success', title: t('workspace.copied') })
    } catch {
      toast({ tone: 'danger', title: t('workspace.copyFailed') })
    }
  }

  const streaming = turn.status === 'streaming'

  return (
    <div className="flex gap-3">
      <span aria-hidden className="mt-0.5 grid size-8 shrink-0 place-items-center rounded-full bg-brand-500 text-ink-on-brand">
        <Sparkles className="size-4" />
      </span>

      <div className="grid min-w-0 flex-1 gap-3">
        {(turn.content || streaming) && (
          <div className="w-full rounded-2xl border border-line-strong bg-surface-card px-4 py-3 shadow-sm">
            {turn.content ? (
              <p className={cn('whitespace-pre-wrap text-sm leading-relaxed [overflow-wrap:anywhere]', streaming && 'after:ml-0.5 after:inline-block after:h-4 after:w-1.5 after:translate-y-0.5 after:animate-pulse after:rounded-sm after:bg-brand-500 after:content-[""]')}>
                {turn.content}
              </p>
            ) : (
              <p role="status" className="flex items-center gap-1.5 text-sm text-ink-soft">
                <span className="size-1.5 animate-bounce rounded-full bg-ink-faint [animation-delay:-0.3s]" />
                <span className="size-1.5 animate-bounce rounded-full bg-ink-faint [animation-delay:-0.15s]" />
                <span className="size-1.5 animate-bounce rounded-full bg-ink-faint" />
                <span className="sr-only">{t('workspace.thinking')}</span>
              </p>
            )}
          </div>
        )}

        {turn.status === 'error' && (
          <p role="alert" className="rounded-md bg-danger-soft px-3 py-2 text-sm text-danger">
            {turn.error ?? t('errors.generic')}
          </p>
        )}
        {turn.status === 'stopped' && <Badge size="sm">{t('workspace.stopped')}</Badge>}

        {turn.suggestions.map((s) => (
          <SuggestionCard
            key={s.id}
            suggestion={s}
            deciding={actions.deciding(s.id)}
            busy={actions.busy}
            onApprove={actions.onApprove}
            onReject={actions.onReject}
          />
        ))}

        {turn.usage && !streaming && <Receipt usage={turn.usage} />}

        {!streaming && turn.content && (
          <div className="-ml-1.5 flex items-center gap-0.5">
            <Tooltip content={t('workspace.copy')}>
              <IconButton aria-label={t('workspace.copy')} onClick={() => void copy()}>
                <Copy />
              </IconButton>
            </Tooltip>
            {isLast && (
              <Tooltip content={t('workspace.regenerate')}>
                <IconButton aria-label={t('workspace.regenerate')} disabled={disabled} onClick={onRegenerate}>
                  <RefreshCw />
                </IconButton>
              </Tooltip>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
