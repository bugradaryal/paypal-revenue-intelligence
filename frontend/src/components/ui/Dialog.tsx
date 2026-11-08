import { useEffect, useId, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'
import { IconButton } from './IconButton'

interface DialogProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children?: ReactNode
  footer?: ReactNode
  /** "drawer" slides in from the right (detail panels); "drawer-left" is used for mobile navigation. */
  variant?: 'modal' | 'drawer' | 'drawer-left'
  closeLabel?: string
}

/**
 * Built on the native <dialog> element: focus trapping, Esc to close and an inert
 * background come from the browser, so no extra dependency is needed.
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  variant = 'modal',
  closeLabel,
}: DialogProps) {
  const { t } = useI18n()
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const el = ref.current
    if (!el) return
    if (open && !el.open) el.showModal()
    if (!open && el.open) el.close()
  }, [open])

  const isLeft = variant === 'drawer-left'
  const isDrawer = variant !== 'modal'

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => {
        // A click on the backdrop targets the <dialog> element itself.
        if (e.target === ref.current) onClose()
      }}
      aria-labelledby={titleId}
      className={cn(
        'fixed m-0 max-h-none max-w-none bg-transparent p-0 text-ink backdrop:bg-black/40 backdrop:backdrop-blur-[2px]',
        !isDrawer && 'inset-0 m-auto h-fit w-[calc(100%-2rem)] max-w-lg',
        isDrawer && !isLeft && 'inset-y-0 right-0 left-auto h-dvh w-full max-w-md',
        isLeft && 'inset-y-0 left-0 right-auto h-dvh w-[min(20rem,85vw)]',
      )}
    >
      <div
        className={cn(
          'flex flex-col border border-line bg-surface-card shadow-lg',
          !isDrawer && 'max-h-[85dvh] rounded-2xl',
          isDrawer && !isLeft && 'h-dvh rounded-none sm:rounded-l-2xl',
          isLeft && 'h-dvh rounded-none rounded-r-2xl',
          open && 'animate-rise-in',
        )}
      >
        <header className="flex items-start justify-between gap-4 border-b border-line px-6 py-5">
          <div className="grid gap-1">
            <h2 id={titleId} className="text-base font-semibold tracking-tight">
              {title}
            </h2>
            {description && <p className="text-sm text-ink-soft">{description}</p>}
          </div>
          <IconButton aria-label={closeLabel ?? t('common.close')} size="sm" onClick={onClose}>
            <X />
          </IconButton>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto px-6 py-5">{children}</div>
        {footer && <footer className="flex justify-end gap-2 border-t border-line px-6 py-4">{footer}</footer>}
      </div>
    </dialog>
  )
}
