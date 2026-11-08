import { useState, type ReactNode } from 'react'
import { Button, Dialog, useToast } from '@/components/ui'
import { useDecideSuggestion } from '@/features/revenue/queries'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import type { RecoverySuggestion } from '@/services/types'

/**
 * The approval rule in one place, shared by Recovery and the AI Workspace:
 * the user's decision is the only thing that lets an AI-proposed action run, and an action that reaches
 * the outside world (executable) asks for an explicit confirmation first.
 */
export function useSuggestionActions(onDecided?: (updated: RecoverySuggestion) => void) {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const decide = useDecideSuggestion()
  const [confirming, setConfirming] = useState<RecoverySuggestion | null>(null)

  function submit(suggestion: RecoverySuggestion, decision: 'approve' | 'reject') {
    decide.mutate(
      { id: suggestion.id, decision },
      {
        onSuccess: (updated) => {
          setConfirming(null)
          onDecided?.(updated)
          toast({
            tone: 'success',
            title:
              decision === 'reject'
                ? t('recovery.toast.rejected')
                : updated.executable
                  ? t('recovery.toast.approvedRunning')
                  : t('recovery.toast.approved'),
          })
        },
        onError: (error) => {
          setConfirming(null)
          toast({ tone: 'danger', title: errorMessage(error) })
        },
      },
    )
  }

  const confirmDialog: ReactNode = (
    <Dialog
      open={confirming !== null}
      onClose={() => setConfirming(null)}
      title={t('recovery.confirm.title')}
      description={confirming ? t('recovery.confirm.body', { customer: confirming.customerName }) : undefined}
      footer={
        <>
          <Button variant="ghost" onClick={() => setConfirming(null)}>
            {t('common.cancel')}
          </Button>
          <Button loading={decide.isPending} onClick={() => confirming && submit(confirming, 'approve')}>
            {t('recovery.confirm.action')}
          </Button>
        </>
      }
    >
      {confirming && <p className="rounded-md bg-surface-muted p-3 text-sm">{confirming.rationale}</p>}
    </Dialog>
  )

  return {
    onApprove: (s: RecoverySuggestion) => (s.executable ? setConfirming(s) : submit(s, 'approve')),
    onReject: (s: RecoverySuggestion) => submit(s, 'reject'),
    deciding: (id: string) => decide.isPending && decide.variables?.id === id,
    busy: decide.isPending,
    confirmDialog,
  }
}
