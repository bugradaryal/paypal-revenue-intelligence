import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

interface EmptyStateProps {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
  tone?: 'neutral' | 'danger'
  className?: string
}

/** Empty and error states share one look so every screen handles "nothing here" the same way. */
export function EmptyState({ icon, title, description, action, tone = 'neutral', className }: EmptyStateProps) {
  return (
    <div className={cn('grid place-items-center gap-2 px-4 py-10 text-center', className)}>
      {icon && (
        <span
          className={cn(
            'mb-1 grid size-11 place-items-center rounded-xl [&_svg]:size-5',
            tone === 'danger' ? 'bg-danger-soft text-danger' : 'bg-surface-muted text-ink-soft',
          )}
        >
          {icon}
        </span>
      )}
      <p className="text-sm font-semibold">{title}</p>
      {description && <p className="max-w-sm text-sm text-ink-soft">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}
