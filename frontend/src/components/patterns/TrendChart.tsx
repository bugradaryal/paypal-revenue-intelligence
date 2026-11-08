import { useCallback, useEffect, useId, useMemo, useRef, useState, type KeyboardEvent, type PointerEvent } from 'react'
import { Table2, LineChart } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from '@/components/ui'
import { useI18n } from '@/i18n'
import { useFormat } from '@/i18n/useFormat'
import { cn } from '@/lib/cn'
import type { Money } from '@/services/types'
import { EmptyState } from './EmptyState'

export interface TrendSeries {
  key: string
  /** Already translated. */
  label: string
  /** CSS colour, normally a chart token such as `var(--chart-1)`. */
  color: string
  /** One value per entry in `dates`: money, or plain counts (a chart never mixes the two). */
  values: (Money | number)[]
}

interface TrendChartProps {
  /** ISO timestamps, one per UTC day. */
  dates: string[]
  series: TrendSeries[]
  /** Already translated, read by screen readers. */
  ariaLabel: string
  height?: number
  /** Digits shown for money values (default 2). */
  moneyDigits?: number
  className?: string
}

const MARGIN = { top: 12, right: 12, bottom: 28, left: 60 }

/** Round up to a "nice" axis maximum and split it into evenly spaced ticks. Display scaling only. */
function niceTicks(max: number, count = 4): number[] {
  if (!(max > 0)) return [0, 1]
  const rough = max / count
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const step = ([1, 2, 2.5, 5, 10].find((m) => m * magnitude >= rough) ?? 10) * magnitude
  const ticks: number[] = []
  for (let v = 0; v <= max + step * 0.999; v += step) ticks.push(Number(v.toFixed(10)))
  return ticks
}

export function TrendChart({ dates, series, ariaLabel, height = 260, moneyDigits, className }: TrendChartProps) {
  const { t } = useI18n()
  const f = useFormat()
  const uid = useId()
  const wrapRef = useRef<HTMLDivElement>(null)
  const [width, setWidth] = useState(0)
  const [active, setActive] = useState<number | null>(null)
  const [tableView, setTableView] = useState(false)

  useEffect(() => {
    const el = wrapRef.current
    if (!el) return
    const observer = new ResizeObserver(([entry]) => setWidth(Math.floor(entry?.contentRect.width ?? 0)))
    observer.observe(el)
    return () => observer.disconnect()
  }, [tableView])

  const first = series[0]?.values[0]
  const isMoney = typeof first === 'object'
  const currency = typeof first === 'object' ? first.currency : 'USD'
  const show = (v: Money | number | undefined) =>
    v === undefined ? '' : typeof v === 'number' ? f.number(v) : f.money(v, { maximumFractionDigits: moneyDigits })

  // Numbers are only used to position marks; every value shown to the user is the backend's Money, formatted.
  const numeric = useMemo(() => series.map((s) => s.values.map((v) => (typeof v === 'number' ? v : Number(v.amount)))), [series])
  const yMax = useMemo(() => Math.max(0, ...numeric.flat().filter(Number.isFinite)), [numeric])
  const ticks = useMemo(() => niceTicks(yMax), [yMax])
  const top = ticks[ticks.length - 1] ?? 1

  const innerW = Math.max(0, width - MARGIN.left - MARGIN.right)
  const innerH = height - MARGIN.top - MARGIN.bottom
  const count = dates.length
  const x = useCallback((i: number) => MARGIN.left + (count <= 1 ? innerW / 2 : (i / (count - 1)) * innerW), [count, innerW])
  const y = useCallback((v: number) => MARGIN.top + innerH - (v / top) * innerH, [innerH, top])

  const hasData = count > 0 && yMax > 0

  const labelEvery = Math.max(1, Math.ceil(count / Math.max(2, Math.floor(innerW / 76))))

  function indexFromPointer(e: PointerEvent<SVGRectElement>) {
    const rect = e.currentTarget.getBoundingClientRect()
    const rel = e.clientX - rect.left
    return Math.min(count - 1, Math.max(0, Math.round((rel / Math.max(1, rect.width)) * (count - 1))))
  }

  function onKeyDown(e: KeyboardEvent<SVGSVGElement>) {
    if (e.key === 'ArrowRight') setActive((a) => Math.min(count - 1, (a ?? -1) + 1))
    else if (e.key === 'ArrowLeft') setActive((a) => Math.max(0, (a ?? count) - 1))
    else if (e.key === 'Escape') setActive(null)
    else return
    e.preventDefault()
  }

  const legend = series.length < 2 ? <span /> : (
    <ul className="flex flex-wrap items-center gap-x-4 gap-y-1">
      {series.map((s) => (
        <li key={s.key} className="flex items-center gap-1.5 text-xs text-ink-soft">
          <span aria-hidden className="h-0.5 w-4 rounded-full" style={{ background: s.color }} />
          {s.label}
        </li>
      ))}
    </ul>
  )

  const toggle = (
    <Button
      size="sm"
      variant="ghost"
      leadingIcon={tableView ? <LineChart className="size-4" /> : <Table2 className="size-4" />}
      onClick={() => setTableView((v) => !v)}
    >
      {tableView ? t('chart.showChart') : t('chart.showTable')}
    </Button>
  )

  return (
    <div className={cn('min-w-0', className)}>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        {legend}
        {hasData && toggle}
      </div>

      {!hasData ? (
        <EmptyState title={t('chart.noData')} description={t('chart.noDataHint')} className="py-14" />
      ) : tableView ? (
        <div className="max-h-[22rem] overflow-y-auto">
          <Table>
            <caption className="sr-only">{ariaLabel}</caption>
            <TableHead>
              <tr>
                <TableHeaderCell>{t('common.date')}</TableHeaderCell>
                {series.map((s) => (
                  <TableHeaderCell key={s.key} numeric>
                    {s.label}
                  </TableHeaderCell>
                ))}
              </tr>
            </TableHead>
            <TableBody>
              {dates.map((d, i) => (
                <TableRow key={d}>
                  <TableCell>{f.day(d, 'long')}</TableCell>
                  {series.map((s) => (
                    <TableCell key={s.key} numeric>
                      {show(s.values[i])}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : (
        <div ref={wrapRef} className="relative" style={{ height }}>
          {width > 0 && (
            <svg
              width={width}
              height={height}
              role="img"
              aria-label={ariaLabel}
              tabIndex={0}
              onKeyDown={onKeyDown}
              onBlur={() => setActive(null)}
              className="focus-ring block overflow-visible rounded-md"
            >
              <defs>
                <linearGradient id={`${uid}-area`} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={series[0]?.color} stopOpacity="0.18" />
                  <stop offset="100%" stopColor={series[0]?.color} stopOpacity="0" />
                </linearGradient>
              </defs>

              {/* Recessive grid and y axis */}
              {ticks.map((tick) => (
                <g key={tick}>
                  <line x1={MARGIN.left} x2={width - MARGIN.right} y1={y(tick)} y2={y(tick)} stroke="var(--line)" strokeDasharray={tick === 0 ? undefined : '3 4'} />
                  <text x={MARGIN.left - 8} y={y(tick)} textAnchor="end" dominantBaseline="middle" className="fill-ink-faint text-[10px]">
                    {isMoney ? f.money({ amount: String(tick), currency }, { maximumFractionDigits: Number.isInteger(tick) ? 0 : 2 }) : f.number(tick)}
                  </text>
                </g>
              ))}

              {dates.map((d, i) =>
                i % labelEvery === 0 ? (
                  <text key={d} x={x(i)} y={height - 8} textAnchor="middle" className="fill-ink-faint text-[10px]">
                    {f.day(d)}
                  </text>
                ) : null,
              )}

              {/* Soft area under the first series only */}
              {series[0] && count > 1 && (
                <path
                  d={`M ${x(0)} ${y(0)} ${(numeric[0] ?? []).map((v, i) => `L ${x(i)} ${y(v)}`).join(' ')} L ${x(count - 1)} ${y(0)} Z`}
                  fill={`url(#${uid}-area)`}
                />
              )}

              {series.map((s, si) => (
                <path
                  key={s.key}
                  d={(numeric[si] ?? []).map((v, i) => `${i === 0 ? 'M' : 'L'} ${x(i)} ${y(v)}`).join(' ')}
                  fill="none"
                  stroke={s.color}
                  strokeWidth={2}
                  strokeLinejoin="round"
                  strokeLinecap="round"
                />
              ))}

              {active !== null && (
                <g pointerEvents="none">
                  <line x1={x(active)} x2={x(active)} y1={MARGIN.top} y2={MARGIN.top + innerH} stroke="var(--line-strong)" />
                  {series.map((s, si) => (
                    <circle key={s.key} cx={x(active)} cy={y(numeric[si]?.[active] ?? 0)} r={4.5} fill={s.color} stroke="var(--surface-card)" strokeWidth={2} />
                  ))}
                </g>
              )}

              {/* Hit area is the whole plot, much bigger than any mark */}
              <rect
                x={MARGIN.left}
                y={MARGIN.top}
                width={innerW}
                height={innerH}
                fill="transparent"
                onPointerMove={(e) => setActive(indexFromPointer(e))}
                onPointerDown={(e) => setActive(indexFromPointer(e))}
                onPointerLeave={(e) => e.pointerType === 'mouse' && setActive(null)}
              />
            </svg>
          )}

          {active !== null && width > 0 && (
            <div
              role="status"
              className="pointer-events-none absolute z-10 min-w-40 rounded-md border border-line bg-surface-raised px-3 py-2 text-xs shadow-md"
              style={{
                top: MARGIN.top,
                left: Math.min(Math.max(x(active) + 12, 0), Math.max(0, width - 176)),
                ...(x(active) > width * 0.62 ? { left: Math.max(0, x(active) - 176) } : {}),
              }}
            >
              <p className="mb-1 font-semibold">{f.day(dates[active] as string, 'long')}</p>
              {series.map((s) => (
                <p key={s.key} className="flex items-center justify-between gap-4">
                  <span className="flex items-center gap-1.5 text-ink-soft">
                    <span aria-hidden className="size-2 rounded-full" style={{ background: s.color }} />
                    {s.label}
                  </span>
                  <span className="tabular font-medium">{show(s.values[active])}</span>
                </p>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
