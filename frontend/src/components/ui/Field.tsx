import { useId, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

/** Shared control chrome so Input, Textarea and Select always look identical. */
export const controlBase = [
  'focus-ring w-full border bg-surface-card text-sm text-ink shadow-xs placeholder:text-ink-faint',
  'transition-[border-color,box-shadow] duration-150 hover:border-line-strong',
  'focus-visible:border-brand-400 disabled:cursor-not-allowed disabled:bg-surface-muted disabled:opacity-60',
].join(' ')

export const controlBorder = (invalid?: boolean) => (invalid ? 'border-danger' : 'border-line')

interface FieldProps {
  label: string
  hint?: string
  error?: string
  required?: boolean
  className?: string
  children: (props: { id: string; describedBy?: string; invalid: boolean }) => ReactNode
}

/** Label + control + hint/error wiring. Pass a render function so ids are connected correctly. */
export function Field({ label, hint, error, required, className, children }: FieldProps) {
  const id = useId()
  const messageId = `${id}-msg`
  const message = error ?? hint

  return (
    <div className={cn('grid gap-1.5', className)}>
      <label htmlFor={id} className="text-xs font-semibold text-ink">
        {label}
        {required && <span className="ml-0.5 text-brand-600">*</span>}
      </label>
      {children({ id, describedBy: message ? messageId : undefined, invalid: Boolean(error) })}
      {message && (
        <p id={messageId} className={cn('text-xs', error ? 'text-danger' : 'text-ink-soft')}>
          {message}
        </p>
      )}
    </div>
  )
}
