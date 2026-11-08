import { cloneElement, useId, useState, type ReactElement, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

interface TooltipProps {
  content: ReactNode
  side?: 'top' | 'bottom'
  /** Allow long text to wrap inside a fixed-width bubble. */
  wrap?: boolean
  /** A single focusable element. It receives aria-describedby and hover/focus handlers. */
  children: ReactElement<Record<string, unknown>>
}

export function Tooltip({ content, side = 'top', wrap, children }: TooltipProps) {
  const id = useId()
  const [open, setOpen] = useState(false)

  const trigger = cloneElement(children, {
    'aria-describedby': open ? id : undefined,
    onMouseEnter: () => setOpen(true),
    onMouseLeave: () => setOpen(false),
    onFocus: () => setOpen(true),
    onBlur: () => setOpen(false),
  })

  return (
    <span className="relative inline-flex">
      {trigger}
      {open && (
        <span
          role="tooltip"
          id={id}
          className={cn(
            'pointer-events-none absolute left-1/2 z-50 -translate-x-1/2 rounded-sm px-2.5 py-1.5',
            'animate-fade-in bg-ink text-xs font-medium text-surface-card shadow-md',
            wrap ? 'w-56 whitespace-normal text-center leading-snug' : 'whitespace-nowrap',
            side === 'top' ? 'bottom-full mb-2' : 'top-full mt-2',
          )}
        >
          {content}
        </span>
      )}
    </span>
  )
}
