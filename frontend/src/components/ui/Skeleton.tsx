import type { HTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

/** Loading placeholder. Size it with className, e.g. <Skeleton className="h-4 w-32" />. */
export function Skeleton({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div aria-hidden className={cn('animate-pulse rounded-md bg-surface-muted', className)} {...props} />
}
