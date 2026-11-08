import { useEffect, useMemo, useRef } from 'react'
import { MessageSquarePlus, Sparkles } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui'
import { ApprovalBanner } from '@/components/patterns'
import { useSuggestionActions } from '@/features/recovery/useSuggestionActions'
import { useSuggestions } from '@/features/revenue/queries'
import { Composer } from '@/features/workspace/Composer'
import { MessageBubble } from '@/features/workspace/MessageBubble'
import { useWorkspaceChat } from '@/features/workspace/useWorkspaceChat'
import { useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'

const STARTERS = ['failures', 'recovery', 'cost', 'help'] as const

export function AiWorkspacePage() {
  const { t } = useI18n()
  const f = useFormat()
  const chat = useWorkspaceChat()
  const actions = useSuggestionActions(chat.syncSuggestion)
  const bottomRef = useRef<HTMLDivElement>(null)
  const nearBottom = useRef(true)

  const proposed = useMemo(
    () => chat.turns.flatMap((turn) => turn.suggestions).filter((s) => s.status === 'proposed'),
    [chat.turns],
  )

  // After approving an executable action, follow it until it has run (the list query polls for at most 30 s).
  const running = chat.turns.some((turn) => turn.suggestions.some((s) => s.status === 'approved' && s.executable))
  const refreshed = useSuggestions({ page: 1, pageSize: 100, status: ['approved', 'executed', 'failed'] }, { enabled: running })
  useEffect(() => {
    refreshed.data?.items.forEach(chat.syncSuggestion)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshed.data])

  // Keep the newest text in view, unless the user scrolled up to read.
  useEffect(() => {
    const onScroll = () => {
      nearBottom.current = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 160
    }
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])
  useEffect(() => {
    if (nearBottom.current) bottomRef.current?.scrollIntoView({ block: 'end' })
  }, [chat.turns])

  const empty = chat.turns.length === 0

  return (
    <>
      <PageHeader
        title={t('pages.aiWorkspace.title')}
        description={t('pages.aiWorkspace.description')}
        actions={
          <Button variant="secondary" size="sm" leadingIcon={<MessageSquarePlus className="size-4" />} disabled={empty} onClick={chat.reset}>
            {t('workspace.newConversation')}
          </Button>
        }
      />

      <div className="grid min-h-[calc(100dvh-15rem)] grid-cols-[minmax(0,1fr)] grid-rows-[1fr_auto] gap-6">
        <div className="grid content-start gap-6">
          {proposed.length > 0 && (
            <ApprovalBanner
              title={t('workspace.banner.title', { count: f.number(proposed.length) })}
              description={t('workspace.banner.description')}
            />
          )}

          {empty ? (
            <section className="grid place-items-center gap-6 py-10 text-center">
              <span className="grid size-14 place-items-center rounded-2xl bg-brand-500 text-ink-on-brand shadow-md">
                <Sparkles className="size-7" />
              </span>
              <div className="grid max-w-md gap-2">
                <h2 className="text-xl font-bold tracking-tight">{t('workspace.empty.title')}</h2>
                <p className="text-sm text-ink-soft">{t('workspace.empty.description')}</p>
              </div>
              <ul className="grid w-full max-w-2xl gap-3 sm:grid-cols-2">
                {STARTERS.map((key) => (
                  <li key={key}>
                    <button
                      type="button"
                      onClick={() => chat.send(t(`workspace.starters.${key}`))}
                      className="focus-ring h-full w-full rounded-xl border border-line bg-surface-card p-4 text-left text-sm font-medium transition-colors hover:border-brand-400 hover:bg-brand-soft"
                    >
                      {t(`workspace.starters.${key}`)}
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          ) : (
            <div role="log" aria-live="polite" aria-label={t('workspace.logLabel')} className="grid gap-6">
              {chat.turns.map((turn, index) => (
                <MessageBubble
                  key={turn.id}
                  turn={turn}
                  isLast={index === chat.turns.length - 1}
                  disabled={chat.streaming}
                  onRegenerate={chat.regenerate}
                  actions={actions}
                />
              ))}
              <div ref={bottomRef} />
            </div>
          )}
        </div>

        <div className="sticky bottom-4 z-10">
          <Composer streaming={chat.streaming} onSend={chat.send} onStop={chat.stop} />
        </div>
      </div>

      {actions.confirmDialog}
    </>
  )
}
