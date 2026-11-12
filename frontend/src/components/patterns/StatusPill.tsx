import { Badge, type BadgeProps } from '@/components/ui'
import { useI18n } from '@/i18n'

type Tone = NonNullable<BadgeProps['tone']>

const PAYMENT_TONE: Record<string, Tone> = {
  completed: 'success',
  pending: 'warning',
  failed: 'danger',
  refunded: 'neutral',
}

const SUGGESTION_TONE: Record<string, Tone> = {
  proposed: 'brand',
  approved: 'info',
  reviewed: 'success',
  rejected: 'neutral',
  executed: 'success',
  failed: 'danger',
}

interface StatusPillProps {
  kind: 'payment' | 'suggestion'
  /** Backend value. An unknown value is shown neutrally instead of breaking the screen. */
  status: string
  size?: BadgeProps['size']
}

/** Status is always label + dot, never colour alone. */
export function StatusPill({ kind, status, size = 'md' }: StatusPillProps) {
  const { tryT } = useI18n()
  const tones = kind === 'payment' ? PAYMENT_TONE : SUGGESTION_TONE
  const namespace = kind === 'payment' ? 'paymentStatus' : 'suggestionStatus'

  return (
    <Badge dot size={size} tone={tones[status] ?? 'neutral'}>
      {tryT(`${namespace}.${status}`, status)}
    </Badge>
  )
}
