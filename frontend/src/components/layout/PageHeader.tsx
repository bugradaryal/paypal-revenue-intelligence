import type { ReactNode } from 'react'

interface PageHeaderProps {
  title: string
  description?: string
  actions?: ReactNode
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="mb-9 flex flex-wrap items-end justify-between gap-5 animate-fade-in">
      <div className="grid max-w-2xl gap-2">
        <span aria-hidden="true" className="h-1 w-10 rounded-full bg-brand-gradient shadow-brand" />
        <h1 className="text-3xl font-extrabold tracking-[-0.045em] sm:text-4xl">{title}</h1>
        {description && <p className="max-w-2xl text-sm leading-6 text-ink-soft sm:text-base">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2 rounded-2xl border border-line/80 bg-surface-card/75 p-1.5 shadow-xs backdrop-blur-sm">{actions}</div>}
    </div>
  )
}
