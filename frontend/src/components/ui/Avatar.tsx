import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

const avatarStyles = cva('inline-grid shrink-0 place-items-center rounded-full bg-brand-soft-strong font-semibold text-brand-text', {
  variants: {
    size: { sm: 'size-7 text-[10px]', md: 'size-9 text-xs', lg: 'size-12 text-sm' },
  },
  defaultVariants: { size: 'md' },
})

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : ''
  return (first + last).toLocaleUpperCase()
}

export function Avatar({ name, size, className }: { name: string; className?: string } & VariantProps<typeof avatarStyles>) {
  return (
    <span aria-hidden className={cn(avatarStyles({ size }), className)}>
      {initials(name)}
    </span>
  )
}
