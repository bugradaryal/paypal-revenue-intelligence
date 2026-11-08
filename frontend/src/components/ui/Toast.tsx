import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import { AlertTriangle, CheckCircle2, Info, X, XCircle } from 'lucide-react'
import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'
import { IconButton } from './IconButton'

type ToastTone = 'success' | 'warning' | 'danger' | 'info'

interface ToastInput {
  title: string
  description?: string
  tone?: ToastTone
  /** Milliseconds before auto-dismiss. 0 keeps it until closed. Default 5000. */
  duration?: number
}

interface ToastItem extends Required<Pick<ToastInput, 'title' | 'tone'>> {
  id: number
  description?: string
}

interface ToastContextValue {
  toast: (input: ToastInput) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

const toneIcon = {
  success: CheckCircle2,
  warning: AlertTriangle,
  danger: XCircle,
  info: Info,
} as const

const toneColor: Record<ToastTone, string> = {
  success: 'text-success',
  warning: 'text-warning',
  danger: 'text-danger',
  info: 'text-info',
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const { t } = useI18n()
  const [items, setItems] = useState<ToastItem[]>([])
  const nextId = useRef(1)

  const dismiss = useCallback((id: number) => setItems((list) => list.filter((t) => t.id !== id)), [])

  const toast = useCallback(
    ({ title, description, tone = 'info', duration = 5000 }: ToastInput) => {
      const id = nextId.current++
      setItems((list) => [...list.slice(-3), { id, title, description, tone }])
      if (duration > 0) window.setTimeout(() => dismiss(id), duration)
    },
    [dismiss],
  )

  const value = useMemo(() => ({ toast }), [toast])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        aria-live="polite"
        className="pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col items-end gap-2 sm:inset-x-auto sm:right-6 sm:bottom-6"
      >
        {items.map((item) => {
          const Icon = toneIcon[item.tone]
          return (
            <div
              key={item.id}
              role={item.tone === 'danger' ? 'alert' : 'status'}
              className="pointer-events-auto flex w-full max-w-sm animate-rise-in items-start gap-3 rounded-lg border border-line bg-surface-raised p-4 shadow-lg"
            >
              <Icon className={cn('mt-0.5 size-5 shrink-0', toneColor[item.tone])} />
              <div className="grid flex-1 gap-0.5">
                <p className="text-sm font-semibold">{item.title}</p>
                {item.description && <p className="text-xs text-ink-soft">{item.description}</p>}
              </div>
              <IconButton aria-label={t('common.dismiss')} size="sm" onClick={() => dismiss(item.id)}>
                <X />
              </IconButton>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used inside <ToastProvider>')
  return ctx
}
