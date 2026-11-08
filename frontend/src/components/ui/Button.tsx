import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'
import { Spinner } from './Spinner'

export const buttonStyles = cva(
  [
    'focus-ring inline-flex shrink-0 select-none items-center justify-center gap-2 whitespace-nowrap font-semibold',
    'transition-[background-color,box-shadow,transform,border-color,color] duration-150 ease-[var(--ease-out-soft)]',
    'active:scale-[0.98] disabled:pointer-events-none disabled:opacity-50',
  ],
  {
    variants: {
      variant: {
        primary: 'bg-brand-gradient text-ink-on-brand shadow-brand hover:brightness-105',
        secondary: 'border border-line-strong bg-surface-card text-ink shadow-xs hover:bg-surface-muted',
        soft: 'bg-brand-soft text-brand-text hover:bg-brand-soft-strong',
        ghost: 'text-ink-soft hover:bg-surface-muted hover:text-ink',
        danger: 'bg-danger text-white shadow-xs hover:brightness-110',
      },
      size: {
        sm: 'h-8 rounded-sm px-3 text-xs',
        md: 'h-10 rounded-md px-4 text-sm',
        lg: 'h-12 rounded-lg px-6 text-base',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
)

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonStyles> {
  loading?: boolean
  leadingIcon?: ReactNode
  trailingIcon?: ReactNode
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { className, variant, size, loading, leadingIcon, trailingIcon, disabled, children, type = 'button', ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={cn(buttonStyles({ variant, size }), className)}
      {...props}
    >
      {loading ? <Spinner className="size-4" /> : leadingIcon}
      {children}
      {!loading && trailingIcon}
    </button>
  )
})
