import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from 'react'
import { Check } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: ReactNode
  description?: ReactNode
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { className, label, description, id, ...props },
  ref,
) {
  const autoId = useId()
  const inputId = id ?? autoId

  return (
    <label htmlFor={inputId} className={cn('group flex cursor-pointer items-start gap-3', className)}>
      <span className="relative mt-0.5 grid size-5 shrink-0 place-items-center">
        <input
          ref={ref}
          id={inputId}
          type="checkbox"
          className="peer focus-ring absolute inset-0 size-5 cursor-pointer appearance-none rounded-xs border border-line-strong bg-surface-card shadow-xs transition-colors checked:border-brand-500 checked:bg-brand-500 disabled:cursor-not-allowed disabled:opacity-50"
          {...props}
        />
        <Check
          className="pointer-events-none relative size-3.5 text-ink-on-brand opacity-0 transition-opacity peer-checked:opacity-100"
          strokeWidth={3}
        />
      </span>
      {(label || description) && (
        <span className="grid gap-0.5 text-sm">
          {label && <span className="font-medium text-ink">{label}</span>}
          {description && <span className="text-xs text-ink-soft">{description}</span>}
        </span>
      )}
    </label>
  )
})
