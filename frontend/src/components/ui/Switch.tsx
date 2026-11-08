import { useId, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

export interface SwitchProps {
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  label?: ReactNode
  description?: ReactNode
  disabled?: boolean
  className?: string
}

export function Switch({ checked, onCheckedChange, label, description, disabled, className }: SwitchProps) {
  const labelId = useId()

  return (
    <div className={cn('flex items-start justify-between gap-4', className)}>
      {(label || description) && (
        <div className="grid gap-0.5 text-sm">
          {label && (
            <span id={labelId} className="font-medium text-ink">
              {label}
            </span>
          )}
          {description && <span className="text-xs text-ink-soft">{description}</span>}
        </div>
      )}
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        aria-labelledby={label ? labelId : undefined}
        disabled={disabled}
        onClick={() => onCheckedChange(!checked)}
        className={cn(
          'focus-ring relative h-6 w-11 shrink-0 rounded-full border transition-colors duration-200',
          'disabled:cursor-not-allowed disabled:opacity-50',
          checked ? 'border-brand-500 bg-brand-500' : 'border-line-strong bg-surface-muted',
        )}
      >
        <span
          className={cn(
            'absolute left-0.5 top-0.5 size-4.5 rounded-full bg-white shadow-sm',
            'transition-transform duration-200 ease-[var(--ease-out-soft)]',
            checked && 'translate-x-5',
          )}
        />
      </button>
    </div>
  )
}
