import { ChevronLeft, ChevronRight } from 'lucide-react'
import { IconButton } from '@/components/ui'
import { useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'

export interface PaginationProps {
  page: number
  pageSize: number
  totalItems: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, pageSize, totalItems, onPageChange }: PaginationProps) {
  const { t } = useI18n()
  const f = useFormat()
  if (totalItems <= 0) return null

  const from = (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalItems)
  const lastPage = Math.max(1, Math.ceil(totalItems / pageSize))

  return (
    <div className="mt-3 flex items-center justify-between gap-3 border-t border-line pt-3 text-sm text-ink-soft">
      <p className="tabular text-xs">{t('table.showing', { from: f.number(from), to: f.number(to), total: f.number(totalItems) })}</p>
      <div className="flex items-center gap-1">
        <IconButton aria-label={t('table.previous')} disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          <ChevronLeft />
        </IconButton>
        <IconButton aria-label={t('table.next')} disabled={page >= lastPage} onClick={() => onPageChange(page + 1)}>
          <ChevronRight />
        </IconButton>
      </div>
    </div>
  )
}
