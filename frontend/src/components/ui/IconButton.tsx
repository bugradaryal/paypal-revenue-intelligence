import { forwardRef, type ButtonHTMLAttributes } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

const iconButtonStyles = cva(
  [
    'focus-ring inline-grid shrink-0 place-items-center transition-colors duration-150',
    'disabled:pointer-events-none disabled:opacity-50',
  ],
  {
    variants: {
      variant: {
        ghost: 'text-ink-soft hover:bg-surface-muted hover:text-ink',
        secondary: 'border border-line-strong bg-surface-card text-ink shadow-xs hover:bg-surface-muted',
        soft: 'bg-brand-soft text-brand-text hover:bg-brand-soft-strong',
      },
      size: {
        sm: 'size-8 rounded-sm [&_svg]:size-4',
        md: 'size-10 rounded-md [&_svg]:size-[1.15rem]',
        lg: 'size-12 rounded-lg [&_svg]:size-5',
      },
    },
    defaultVariants: { variant: 'ghost', size: 'md' },
  },
)

export interface IconButtonProps
  extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'aria-label'>,
    VariantProps<typeof iconButtonStyles> {
  /** Required: icon-only buttons need an accessible name. */
  'aria-label': string
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(function IconButton(
  { className, variant, size, type = 'button', ...props },
  ref,
) {
  return <button ref={ref} type={type} className={cn(iconButtonStyles({ variant, size }), className)} {...props} />
})
