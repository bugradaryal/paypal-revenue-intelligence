import type { ReactNode } from 'react'
import { ShieldCheck } from 'lucide-react'

interface ApprovalBannerProps {
  /** Already translated. */
  title: string
  description: string
  action?: ReactNode
}

/**
 * Shown wherever the AI has proposed something that touches the outside world.
 * It states the rule plainly: nothing happens until the user approves.
 */
export function ApprovalBanner({ title, description, action }: ApprovalBannerProps) {
  return (
    <div
      role="region"
      aria-label={title}
      className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-brand-200 bg-brand-soft p-4 dark:border-brand-800"
    >
      <div className="flex min-w-0 items-start gap-3">
        <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-brand-soft-strong text-brand-text">
          <ShieldCheck className="size-5" />
        </span>
        <div className="grid gap-0.5">
          <p className="text-sm font-semibold">{title}</p>
          <p className="text-sm text-ink-soft">{description}</p>
        </div>
      </div>
      {action}
    </div>
  )
}
