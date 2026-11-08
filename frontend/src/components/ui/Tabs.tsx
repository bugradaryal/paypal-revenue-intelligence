import { createContext, useCallback, useContext, useId, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

interface TabsContextValue {
  value: string
  setValue: (v: string) => void
  baseId: string
}

const TabsContext = createContext<TabsContextValue | null>(null)

function useTabs() {
  const ctx = useContext(TabsContext)
  if (!ctx) throw new Error('Tabs parts must be used inside <Tabs>')
  return ctx
}

interface TabsProps {
  /** Controlled value. Omit to let Tabs manage its own state. */
  value?: string
  defaultValue?: string
  onValueChange?: (value: string) => void
  children: ReactNode
  className?: string
}

export function Tabs({ value, defaultValue = '', onValueChange, children, className }: TabsProps) {
  const [inner, setInner] = useState(defaultValue)
  const baseId = useId()
  const current = value ?? inner

  const setValue = useCallback(
    (v: string) => {
      if (value === undefined) setInner(v)
      onValueChange?.(v)
    },
    [value, onValueChange],
  )

  return (
    <TabsContext.Provider value={{ value: current, setValue, baseId }}>
      <div className={className}>{children}</div>
    </TabsContext.Provider>
  )
}

export function TabsList({ children, className, label }: { children: ReactNode; className?: string; label?: string }) {
  const ref = useRef<HTMLDivElement>(null)

  function onKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    const tabs = Array.from(ref.current?.querySelectorAll<HTMLButtonElement>('[role="tab"]:not(:disabled)') ?? [])
    const index = tabs.findIndex((t) => t === document.activeElement)
    if (index < 0) return
    let next = index
    if (e.key === 'ArrowRight') next = (index + 1) % tabs.length
    else if (e.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length
    else if (e.key === 'Home') next = 0
    else if (e.key === 'End') next = tabs.length - 1
    else return
    e.preventDefault()
    tabs[next]?.focus()
    tabs[next]?.click()
  }

  return (
    <div
      ref={ref}
      role="tablist"
      aria-label={label}
      onKeyDown={onKeyDown}
      className={cn('inline-flex items-center gap-1 rounded-lg bg-surface-muted p-1', className)}
    >
      {children}
    </div>
  )
}

export function Tab({ value, children, disabled }: { value: string; children: ReactNode; disabled?: boolean }) {
  const { value: current, setValue, baseId } = useTabs()
  const active = current === value

  return (
    <button
      type="button"
      role="tab"
      id={`${baseId}-tab-${value}`}
      aria-selected={active}
      aria-controls={`${baseId}-panel-${value}`}
      tabIndex={active ? 0 : -1}
      disabled={disabled}
      onClick={() => setValue(value)}
      className={cn(
        'focus-ring h-8 rounded-md px-3.5 text-xs font-semibold transition-[background-color,color,box-shadow] duration-150',
        'disabled:pointer-events-none disabled:opacity-50',
        active ? 'bg-surface-card text-ink shadow-sm' : 'text-ink-soft hover:text-ink',
      )}
    >
      {children}
    </button>
  )
}

export function TabsPanel({ value, children, className }: { value: string; children: ReactNode; className?: string }) {
  const { value: current, baseId } = useTabs()
  if (current !== value) return null

  return (
    <div
      role="tabpanel"
      id={`${baseId}-panel-${value}`}
      aria-labelledby={`${baseId}-tab-${value}`}
      tabIndex={0}
      className={cn('focus-ring animate-fade-in rounded-md', className)}
    >
      {children}
    </div>
  )
}
