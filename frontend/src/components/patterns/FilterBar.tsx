import { useId, type ReactNode } from 'react'
import { Search, X } from 'lucide-react'
import { Button, Input } from '@/components/ui'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'

/** One row of filters above a table or chart. */
export function FilterBar({
  children,
  onReset,
  canReset,
  className,
}: {
  children: ReactNode
  onReset?: () => void
  canReset?: boolean
  className?: string
}) {
  const { t } = useI18n()
  return (
    <div className={cn('flex flex-wrap items-center gap-3', className)}>
      {children}
      {onReset && canReset && (
        <Button variant="ghost" size="sm" leadingIcon={<X className="size-4" />} onClick={onReset}>
          {t('common.reset')}
        </Button>
      )}
    </div>
  )
}

export function SearchField({
  value,
  onChange,
  placeholder,
  label,
  className,
}: {
  value: string
  onChange: (value: string) => void
  placeholder: string
  /** Accessible name. */
  label: string
  className?: string
}) {
  return (
    <div className={cn('w-full sm:w-72', className)}>
      <Input
        type="search"
        aria-label={label}
        placeholder={placeholder}
        value={value}
        leadingIcon={<Search />}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  )
}

interface Option<T extends string> {
  value: T
  label: string
}

/** Single choice, e.g. a time range. */
export function SegmentedControl<T extends string>({
  value,
  options,
  onChange,
  label,
}: {
  value: T
  options: Option<T>[]
  onChange: (value: T) => void
  label: string
}) {
  const name = useId()
  return (
    <div role="radiogroup" aria-label={label} className="inline-flex rounded-md border border-line bg-surface-muted p-0.5">
      {options.map((option) => {
        const selected = option.value === value
        return (
          <label
            key={option.value}
            className={cn(
              'relative cursor-pointer select-none rounded-sm px-3 py-1.5 text-sm font-medium transition-colors',
              'has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-[var(--focus-ring)]',
              selected ? 'bg-surface-card text-ink shadow-xs' : 'text-ink-soft hover:text-ink',
            )}
          >
            <input
              type="radio"
              name={name}
              value={option.value}
              checked={selected}
              onChange={() => onChange(option.value)}
              className="absolute inset-0 size-full cursor-pointer opacity-0"
            />
            {option.label}
          </label>
        )
      })}
    </div>
  )
}

/** Toggle chips for multiple choices or a single selected value. An empty selection means "all". */
export function ChipFilter<T extends string>({
  options,
  value,
  onChange,
  label,
  selectionMode = 'multiple',
}: {
  options: Option<T>[]
  value: T[]
  onChange: (value: T[]) => void
  label: string
  selectionMode?: 'single' | 'multiple'
}) {
  const toggle = (item: T) => {
    if (selectionMode === 'single') {
      onChange(value[0] === item ? value : [item])
      return
    }

    onChange(value.includes(item) ? value.filter((v) => v !== item) : [...value, item])
  }

  return (
    <div role="group" aria-label={label} className="flex flex-wrap gap-1.5">
      {options.map((option) => {
        const pressed = value.includes(option.value)
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={pressed}
            onClick={() => toggle(option.value)}
            className={cn(
              'focus-ring rounded-full border px-3 py-1 text-xs font-medium transition-colors',
              pressed
                ? 'border-brand-400 bg-brand-soft text-brand-text'
                : selectionMode === 'single' && value.length
                  ? 'border-line bg-surface-muted text-ink-faint hover:border-brand-300 hover:bg-brand-soft hover:text-brand-text'
                  : 'border-line bg-surface-card text-ink-soft hover:border-line-strong hover:text-ink',
            )}
          >
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
