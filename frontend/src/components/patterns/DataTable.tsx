import type { ReactNode } from 'react'
import { AlertTriangle, ArrowDown, ArrowUp, Inbox, RefreshCw } from 'lucide-react'
import {
  Button,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from '@/components/ui'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'
import { EmptyState } from './EmptyState'
import { Pagination } from './Pagination'

export interface Column<T> {
  key: string
  /** Already translated. */
  header: string
  cell: (row: T) => ReactNode
  numeric?: boolean
  /** Makes the column sortable. The value is what the backend expects as `sortBy`. */
  sortKey?: string
  /** Hide on small screens to keep the table readable. */
  hideBelow?: 'sm' | 'md'
  className?: string
}

export interface SortState {
  by: string
  dir: 'asc' | 'desc'
}

interface DataTableProps<T> {
  columns: Column<T>[]
  rows: T[] | undefined
  getRowId: (row: T) => string
  /** Visually hidden table title for screen readers. */
  caption: string
  tableClassName?: string
  loading?: boolean
  /** Refetching while old rows are still shown. */
  fetching?: boolean
  /** Already translated message. */
  error?: string
  onRetry?: () => void
  emptyTitle: string
  emptyDescription?: string
  sort?: SortState
  onSortChange?: (sort: SortState) => void
  pagination?: { page: number; pageSize: number; totalItems: number; onPageChange: (page: number) => void }
  onRowClick?: (row: T) => void
  skeletonRows?: number
}

const HIDE = { sm: 'max-sm:hidden', md: 'max-md:hidden' } as const

export function DataTable<T>({
  columns,
  rows,
  getRowId,
  caption,
  tableClassName,
  loading,
  fetching,
  error,
  onRetry,
  emptyTitle,
  emptyDescription,
  sort,
  onSortChange,
  pagination,
  onRowClick,
  skeletonRows = 5,
}: DataTableProps<T>) {
  const { t } = useI18n()

  if (error && !rows?.length) {
    return (
      <EmptyState
        tone="danger"
        icon={<AlertTriangle />}
        title={error}
        action={
          onRetry && (
            <Button size="sm" variant="secondary" leadingIcon={<RefreshCw className="size-4" />} onClick={onRetry}>
              {t('common.retry')}
            </Button>
          )
        }
      />
    )
  }

  const showRows = !loading && rows
  const empty = showRows && rows.length === 0

  function nextSort(column: Column<T>) {
    if (!column.sortKey || !onSortChange) return
    const dir = sort?.by === column.sortKey && sort.dir === 'desc' ? 'asc' : 'desc'
    onSortChange({ by: column.sortKey, dir })
  }

  return (
    <div>
      {empty ? (
        <EmptyState icon={<Inbox />} title={emptyTitle} description={emptyDescription} />
      ) : (
        <div className={cn('transition-opacity', fetching && 'opacity-60')} aria-busy={loading || fetching || undefined}>
          <Table className={tableClassName}>
            <caption className="sr-only">{caption}</caption>
            <TableHead>
              <tr>
                {columns.map((column) => {
                  const sorted = column.sortKey && sort?.by === column.sortKey ? sort.dir : undefined
                  return (
                    <TableHeaderCell
                      key={column.key}
                      numeric={column.numeric}
                      aria-sort={sorted ? (sorted === 'asc' ? 'ascending' : 'descending') : undefined}
                      className={cn(column.hideBelow && HIDE[column.hideBelow], column.className)}
                    >
                      {column.sortKey && onSortChange ? (
                        <button
                          type="button"
                          onClick={() => nextSort(column)}
                          className={cn(
                            'focus-ring -mx-1 inline-flex items-center gap-1 rounded px-1 uppercase tracking-wide hover:text-ink',
                            sorted && 'text-ink',
                          )}
                        >
                          {column.header}
                          {sorted === 'asc' ? <ArrowUp className="size-3.5" /> : sorted === 'desc' ? <ArrowDown className="size-3.5" /> : null}
                          <span className="sr-only">{t('table.sortBy', { column: column.header })}</span>
                        </button>
                      ) : (
                        column.header
                      )}
                    </TableHeaderCell>
                  )
                })}
              </tr>
            </TableHead>
            <TableBody>
              {loading || !rows
                ? Array.from({ length: skeletonRows }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      {columns.map((column) => (
                        <TableCell key={column.key} numeric={column.numeric} className={cn(column.hideBelow && HIDE[column.hideBelow])}>
                          <Skeleton className={cn('h-4', column.numeric ? 'ml-auto w-16' : 'w-28')} />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : rows.map((row) => (
                    <TableRow
                      key={getRowId(row)}
                      onClick={onRowClick ? () => onRowClick(row) : undefined}
                      onKeyDown={
                        onRowClick
                          ? (e) => {
                              if (e.key === 'Enter') onRowClick(row)
                            }
                          : undefined
                      }
                      tabIndex={onRowClick ? 0 : undefined}
                      className={cn(onRowClick && 'focus-ring cursor-pointer')}
                    >
                      {columns.map((column) => (
                        <TableCell
                          key={column.key}
                          numeric={column.numeric}
                          className={cn(column.hideBelow && HIDE[column.hideBelow], column.className)}
                        >
                          {column.cell(row)}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
            </TableBody>
          </Table>
        </div>
      )}

      {pagination && <Pagination {...pagination} />}
    </div>
  )
}
