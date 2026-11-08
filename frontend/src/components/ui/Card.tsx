import type { HTMLAttributes, ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

const cardStyles = cva('rounded-xl border border-line', {
  variants: {
    variant: {
      default: 'bg-surface-card shadow-sm',
      muted: 'bg-surface-muted',
      gradient: 'bg-surface-gradient shadow-sm',
      brand: 'border-transparent bg-brand-gradient text-ink-on-brand shadow-brand',
    },
    padding: { none: '', sm: 'p-4', md: 'p-6' },
    interactive: {
      true: 'cursor-pointer transition-[box-shadow,transform,border-color] duration-200 hover:-translate-y-0.5 hover:border-line-strong hover:shadow-md',
      false: '',
    },
  },
  defaultVariants: { variant: 'default', padding: 'md', interactive: false },
})

export interface CardProps extends HTMLAttributes<HTMLDivElement>, VariantProps<typeof cardStyles> {}

export function Card({ className, variant, padding, interactive, ...props }: CardProps) {
  return <div className={cn(cardStyles({ variant, padding, interactive }), className)} {...props} />
}

interface CardHeaderProps {
  title: ReactNode
  description?: ReactNode
  action?: ReactNode
  className?: string
}

export function CardHeader({ title, description, action, className }: CardHeaderProps) {
  return (
    <div className={cn('mb-5 flex flex-wrap items-start justify-between gap-x-4 gap-y-3', className)}>
      <div className="grid min-w-0 gap-1">
        <h3 className="text-sm font-semibold tracking-tight text-ink">{title}</h3>
        {description && <p className="text-xs text-ink-soft">{description}</p>}
      </div>
      {action}
    </div>
  )
}
