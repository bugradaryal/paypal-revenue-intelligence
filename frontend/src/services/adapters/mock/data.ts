/**
 * The local demo dataset is fictional and generated deterministically. It stands in for
 * operations that are not yet available through the API; it contains no real payments,
 * customers, or measured AI savings.
 */
import { getCurrentLocale } from '@/i18n/current'
import { DAY_MS, startOfUtcDay } from '@/lib/dates'
import { formatMoney, formatPercent } from '@/lib/format'
import type {
  AiBudgetSetting,
  AiCostBreakdown,
  AiCostBreakdownItem,
  AiCostPoint,
  AiModelInfo,
  AiUsageSummary,
  CashFlowOutlook,
  DateRange,
  Money,
  Payment,
  PaymentDetail,
  RevenueInsight,
  RecoverySuggestion,
  RecoverySuggestionType,
  RecoverySummary,
  RevenueAnalytics,
  RevenueIssue,
  RevenuePoint,
  RevenueSummary,
} from '../../types'
import { mulberry32, pick } from './random'

const CURRENCY = 'USD'

const centsToMoney = (cents: number): Money => ({ amount: (cents / 100).toFixed(2), currency: CURRENCY })
const usdToMoney = (usd: number): Money => ({ amount: usd.toFixed(2), currency: CURRENCY })
const moneyFromCents = (cents: number, currency: string): Money => ({ amount: (cents / 100).toFixed(2), currency })

const inRange = (ms: number, range: DateRange) => ms >= Date.parse(range.from) && ms < Date.parse(range.to)

/** Whole UTC days covered by a range. */
function eachDay(range: DateRange): number[] {
  const days: number[] = []
  for (let d = startOfUtcDay(Date.parse(range.from)); d < Date.parse(range.to); d += DAY_MS) days.push(d)
  return days
}

// ---------------------------------------------------------------------------
// Payments
// ---------------------------------------------------------------------------

interface MockPayment extends Payment {
  cents: number
  recoveredFromFailure: boolean
}

const CUSTOMERS = [
  'Acme GmbH',
  'Nova Studio',
  'Lumen Labs',
  'Kestrel Foods',
  'Orbit Logistics',
  'Verde Interiors',
  'Pixel Forge',
  'Harbor & Co',
  'Atlas Fitness',
  'Mira Boutique',
  'Delta Dental',
  'Sunrise Bakery',
  'Northstar Supply',
  'Ember & Oak',
  'Pioneer Health',
  'Bluebird Travel',
  'Cedar Works',
  'Brightline Media',
  'Summit Office',
  'Willow Market',
  'Silverline Auto',
  'Meadow & Main',
  'Clearwater Studio',
  'Redwood Services',
  'Juniper Coffee',
  'Coastal Design',
  'Evergreen Goods',
  'Ironwood Labs',
  'Daybreak Fitness',
  'Cobalt Consulting',
  'Maple Street Books',
  'Beacon Events',
  'Golden Hour Photo',
] as const

const FAILURE_CODES = ['card_declined', 'insufficient_funds', 'expired_card', 'account_restricted'] as const

let paymentCache: MockPayment[] | null = null

export function getMockPayments(): MockPayment[] {
  if (paymentCache) return paymentCache

  const rnd = mulberry32(20261106)
  const now = Date.now()
  const generated: Omit<MockPayment, 'id'>[] = []

  for (let i = 0; i < 260; i++) {
    const createdMs = Math.min(now - 60_000, startOfUtcDay(now) + DAY_MS - Math.floor(rnd() * 120 * DAY_MS))
    const ageDays = (now - createdMs) / DAY_MS
    const customerIndex = Math.floor(rnd() * CUSTOMERS.length)
    const cents = Math.round((20 + rnd() ** 2 * 2400) * 100)

    const r = rnd()
    let status: Payment['status'] = 'completed'
    if (r >= 0.78 && r < 0.9) status = 'failed'
    else if (r >= 0.9 && r < 0.97) status = ageDays < 3 ? 'pending' : 'completed'
    else if (r >= 0.97) status = 'refunded'

    const recovered = status === 'completed' && rnd() < 0.07
    const updatedMs = Math.min(now, createdMs + (recovered ? (1 + rnd() * 2) * DAY_MS : rnd() * 6 * 3_600_000))

    generated.push({
      customerId: `cus_${String(customerIndex + 1).padStart(3, '0')}`,
      customerName: CUSTOMERS[customerIndex] as string,
      amount: centsToMoney(cents),
      status,
      failureCode: status === 'failed' ? pick(FAILURE_CODES, rnd) : null,
      paypalTransactionId: null,
      dataSource: 'demo',
      createdAt: new Date(createdMs).toISOString(),
      updatedAt: new Date(updatedMs).toISOString(),
      cents,
      recoveredFromFailure: recovered,
    })
  }

  generated.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
  paymentCache = generated.map((p, index) => ({ ...p, id: `PAY-${2000 + generated.length - index}` }))
  return paymentCache
}

/** Strip mock-only fields. */
export function toPayment(p: MockPayment): Payment {
  return {
    id: p.id,
    customerId: p.customerId,
    customerName: p.customerName,
    amount: p.amount,
    status: p.status,
    failureCode: p.failureCode,
    paypalTransactionId: p.paypalTransactionId,
    dataSource: p.dataSource,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
  }
}

export function toPaymentDetail(p: MockPayment): PaymentDetail {
  const history: PaymentDetail['statusHistory'] = [{ status: 'pending', occurredAt: p.createdAt }]
  if (p.status !== 'pending') history.push({ status: p.status, occurredAt: p.updatedAt })
  return { ...toPayment(p), statusHistory: history }
}

export function getMockIssues(): RevenueIssue[] {
  return getMockPayments()
    .filter((payment) => payment.status === 'failed' || payment.status === 'pending' || payment.recoveredFromFailure)
    .map((payment) => {
      const recovered = payment.recoveredFromFailure
      return {
        id: `issue_${payment.id}`,
        type: payment.status === 'pending' ? 'pending_payment' : 'failed_payment',
        status: recovered ? 'recovered' : 'open',
        paymentId: payment.id,
        invoiceId: null,
        customerId: payment.customerId,
        customerName: payment.customerName,
        amount: payment.amount,
        recoveredAmount: recovered ? payment.amount : moneyFromCents(0, payment.amount.currency),
        failureCode: payment.failureCode,
        dataSource: 'demo',
        problemSince: payment.createdAt,
        detectedAt: payment.updatedAt,
        resolvedAt: recovered ? payment.updatedAt : null,
        evidence: recovered ? 'status_changed' : null,
      }
    })
}

export function buildRevenueSummary(range: DateRange): RevenueSummary {
  const payments = getMockPayments().filter((p) => inRange(Date.parse(p.createdAt), range))

  let revenue = 0
  let failed = 0
  let pending = 0
  let recovered = 0
  let failedCount = 0
  let pendingCount = 0

  const daily = new Map<number, { revenue: number; recovered: number }>()
  for (const d of eachDay(range)) daily.set(d, { revenue: 0, recovered: 0 })

  for (const p of payments) {
    const bucket = daily.get(startOfUtcDay(Date.parse(p.createdAt)))
    if (p.status === 'completed') {
      revenue += p.cents
      if (bucket) bucket.revenue += p.cents
      if (p.recoveredFromFailure) {
        recovered += p.cents
        if (bucket) bucket.recovered += p.cents
      }
    } else if (p.status === 'failed') {
      failed += p.cents
      failedCount++
    } else if (p.status === 'pending') {
      pending += p.cents
      pendingCount++
    }
  }

  const series: RevenuePoint[] = [...daily.entries()].map(([date, v]) => ({
    date: new Date(date).toISOString(),
    revenue: centsToMoney(v.revenue),
    recovered: centsToMoney(v.recovered),
  }))

  return {
    range,
    totalRevenue: centsToMoney(revenue),
    failedAmount: centsToMoney(failed),
    pendingAmount: centsToMoney(pending),
    recoveredRevenue: centsToMoney(recovered),
    paymentCount: payments.length,
    failedCount,
    pendingCount,
    excludedPaymentCount: 0,
    series,
  }
}

export function buildMockRevenueAnalytics(range: DateRange): RevenueAnalytics {
  const allPayments = getMockPayments()
  const from = Date.parse(range.from)
  const to = Date.parse(range.to)
  const inRange = allPayments.filter((payment) => inRangeTime(Date.parse(payment.createdAt), from, to))
  const usdPayments = inRange.filter((payment) => payment.amount.currency === CURRENCY)
  const completed = usdPayments.filter((payment) => payment.status === 'completed')
  const failed = usdPayments.filter((payment) => payment.status === 'failed')
  const pending = usdPayments.filter((payment) => payment.status === 'pending')
  const refunded = usdPayments.filter((payment) => payment.status === 'refunded')
  const asOfDateUtc = new Date().toISOString().slice(0, 10)
  const today = Date.parse(`${asOfDateUtc}T00:00:00Z`)
  const todayDate = new Date(today)
  const currentMonth = Date.UTC(todayDate.getUTCFullYear(), todayDate.getUTCMonth(), 1)
  const cohortStartDate = new Date(currentMonth)
  cohortStartDate.setUTCMonth(cohortStartDate.getUTCMonth() - 6)
  const cohortStart = cohortStartDate.getTime()

  const daily: RevenueAnalytics['daily'] = eachDay(range).map((day) => {
    const records = usdPayments.filter((payment) => startOfUtcDay(Date.parse(payment.createdAt)) === day)
    return {
      date: new Date(day).toISOString().slice(0, 10),
      completedAmount: centsToMoney(sumCents(records, 'completed')),
      failedAmount: centsToMoney(sumCents(records, 'failed')),
      pendingAmount: centsToMoney(sumCents(records, 'pending')),
      refundedAmount: centsToMoney(sumCents(records, 'refunded')),
    }
  })

  const reasonGroups = new Map<string, MockPayment[]>()
  for (const payment of inRange.filter((record) => record.status === 'failed')) {
    const code = payment.failureCode ?? 'other'
    reasonGroups.set(code, [...(reasonGroups.get(code) ?? []), payment])
  }
  const failureReasons = [...reasonGroups.entries()]
    .map(([code, records]) => ({
      code,
      count: records.length,
      amountUsd: centsToMoney(records.filter((record) => record.amount.currency === CURRENCY).reduce((sum, record) => sum + record.cents, 0)),
    }))
    .sort((left, right) => right.count - left.count || left.code.localeCompare(right.code))

  const trendGroups = new Map<string, number>()
  for (const payment of inRange.filter((record) => record.status === 'failed')) {
    const date = payment.createdAt.slice(0, 10)
    const code = payment.failureCode ?? 'other'
    const key = `${date}|${code}`
    trendGroups.set(key, (trendGroups.get(key) ?? 0) + 1)
  }
  const failureTrend = [...trendGroups.entries()]
    .map(([key, count]) => {
      const [date, code] = key.split('|')
      return { date: date as string, code: code as string, count }
    })
    .sort((left, right) => left.date.localeCompare(right.date) || left.code.localeCompare(right.code))

  const firstSuccessful = new Map<string, number>()
  for (const payment of allPayments) {
    if (payment.status !== 'completed' || payment.amount.currency !== CURRENCY) continue
    const at = Date.parse(payment.createdAt)
    const previous = firstSuccessful.get(payment.customerId)
    if (previous === undefined || at < previous) firstSuccessful.set(payment.customerId, at)
  }
  const customersByCohort = new Map<number, string[]>()
  for (const [customerId, at] of firstSuccessful) {
    if (at < cohortStart || at >= currentMonth) continue
    const date = new Date(at)
    const month = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1)
    customersByCohort.set(month, [...(customersByCohort.get(month) ?? []), customerId])
  }

  const cohorts: RevenueAnalytics['cohorts'] = [...customersByCohort.entries()]
    .sort(([left], [right]) => left - right)
    .map(([cohortMonth, customerIds]) => {
      const periods: RevenueAnalytics['cohorts'][number]['periods'] = []
      for (let month = cohortMonth; month < currentMonth; month = nextMonth(month)) {
        const active = new Set<string>()
        let revenueCents = 0
        for (const payment of allPayments) {
          if (payment.status !== 'completed' || payment.amount.currency !== CURRENCY) continue
          const at = Date.parse(payment.createdAt)
          const date = new Date(at)
          const paymentMonth = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1)
          if (paymentMonth === month && customerIds.includes(payment.customerId)) {
            active.add(payment.customerId)
            revenueCents += payment.cents
          }
        }
        periods.push({
          ageInMonths: monthDifference(cohortMonth, month),
          activeCustomers: active.size,
          retentionRate: active.size / customerIds.length,
          revenuePerCustomer: centsToMoney(Math.round(revenueCents / customerIds.length)),
        })
      }
      return {
        cohortMonth: new Date(cohortMonth).toISOString().slice(0, 10),
        cohortSize: customerIds.length,
        periods,
      }
    })

  const currentStart = today - 14 * DAY_MS
  const baselineStart = today - 28 * DAY_MS
  const baseline = allPayments.filter((payment) => {
    const at = Date.parse(payment.createdAt)
    return at >= baselineStart && at < currentStart
  })
  const current = allPayments.filter((payment) => {
    const at = Date.parse(payment.createdAt)
    return at >= currentStart && at < today
  })
  const baselineFailed = baseline.filter((payment) => payment.status === 'failed').length
  const currentFailed = current.filter((payment) => payment.status === 'failed').length
  const baselineRate = baseline.length ? baselineFailed / baseline.length : 0
  const currentRate = current.length ? currentFailed / current.length : 0
  const failureRateSpike = baseline.length >= 10 && current.length >= 10 && currentFailed >= 3 && currentRate - baselineRate >= 0.1
    ? {
        currentRate,
        baselineRate,
        currentFailedCount: currentFailed,
        currentPaymentCount: current.length,
        baselinePaymentCount: baseline.length,
      }
    : null
  const baselineByCurrency = completedByCurrency(baseline)
  const currentByCurrency = completedByCurrency(current)
  const revenueDrops = [...baselineByCurrency.entries()]
    .filter(([, value]) => value.count >= 5 && value.cents > 0)
    .map(([currency, value]) => {
      const currentValue = currentByCurrency.get(currency) ?? { cents: 0, count: 0 }
      return {
        currency,
        currentRevenue: moneyFromCents(currentValue.cents, currency),
        baselineRevenue: moneyFromCents(value.cents, currency),
        changePercent: ((currentValue.cents - value.cents) / value.cents) * 100,
        currentPaymentCount: currentValue.count,
        baselinePaymentCount: value.count,
      }
    })
    .filter((drop) => drop.changePercent <= -40)
    .sort((left, right) => left.changePercent - right.changePercent)

  const cashFlow = buildMockCashFlowOutlook()
  return {
    range,
    asOfDateUtc,
    leakage: {
      transactionActivity: centsToMoney(sumStatusCents(usdPayments)),
      completedAmount: centsToMoney(sumCents(usdPayments, 'completed')),
      completedCount: completed.length,
      failedAttemptAmount: centsToMoney(sumCents(usdPayments, 'failed')),
      failedCount: failed.length,
      pendingAmount: centsToMoney(sumCents(usdPayments, 'pending')),
      pendingCount: pending.length,
      refundedAmount: centsToMoney(sumCents(usdPayments, 'refunded')),
      refundedCount: refunded.length,
      currentOpenReceivables: cashFlow.openReceivables,
      currentOpenInvoiceCount: cashFlow.openInvoiceCount,
      currentOverdueReceivables: cashFlow.overdueReceivables,
      currentOverdueInvoiceCount: cashFlow.overdueInvoiceCount,
      cancellationTrackingAvailable: false,
    },
    daily,
    failureReasons,
    failureTrend,
    cohorts,
    anomalies: { windowDays: 14, failureRateSpike, revenueDrops },
    excludedPaymentCount: inRange.filter((payment) => payment.amount.currency !== CURRENCY).length,
  }
}

function inRangeTime(timestamp: number, from: number, to: number) {
  return timestamp >= from && timestamp < to
}

function sumCents(payments: MockPayment[], status: Payment['status']) {
  return payments.filter((payment) => payment.status === status).reduce((sum, payment) => sum + payment.cents, 0)
}

function sumStatusCents(payments: MockPayment[]) {
  return payments.reduce((sum, payment) => sum + payment.cents, 0)
}

function monthDifference(from: number, to: number) {
  const first = new Date(from)
  const second = new Date(to)
  return (second.getUTCFullYear() - first.getUTCFullYear()) * 12 + second.getUTCMonth() - first.getUTCMonth()
}

function nextMonth(timestamp: number) {
  const date = new Date(timestamp)
  return Date.UTC(date.getUTCFullYear(), date.getUTCMonth() + 1, 1)
}

function completedByCurrency(payments: MockPayment[]) {
  const totals = new Map<string, { cents: number; count: number }>()
  for (const payment of payments) {
    if (payment.status !== 'completed') continue
    const current = totals.get(payment.amount.currency) ?? { cents: 0, count: 0 }
    totals.set(payment.amount.currency, { cents: current.cents + payment.cents, count: current.count + 1 })
  }
  return totals
}

  export function buildMockCashFlowOutlook(): CashFlowOutlook {
    const today = new Date().toISOString().slice(0, 10)
    const todayMs = Date.parse(`${today}T00:00:00Z`)
    const records = [
      { id: 'demo-inv-1', invoiceNumber: 'INV-3184', customerName: 'Verde Interiors', amountCents: 129550, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: -4 },
      { id: 'demo-inv-2', invoiceNumber: 'INV-3172', customerName: 'Harbor & Co', amountCents: 275000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: -12 },
      { id: 'demo-inv-3', invoiceNumber: 'INV-3141', customerName: 'Sunrise Bakery', amountCents: 45000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: -45 },
      { id: 'demo-inv-4', invoiceNumber: 'INV-3201', customerName: 'Atlas Fitness', amountCents: 83540, paidCents: 0, currency: 'USD', status: 'payment_pending' as const, dueOffset: 3 },
      { id: 'demo-inv-5', invoiceNumber: 'INV-3210', customerName: 'Nova Studio', amountCents: 125000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: 9 },
      { id: 'demo-inv-6', invoiceNumber: 'INV-3224', customerName: 'Lumen Labs', amountCents: 150000, paidCents: 50000, currency: 'USD', status: 'partially_paid' as const, dueOffset: 5 },
      { id: 'demo-inv-7', invoiceNumber: 'INV-3238', customerName: 'Kestrel Foods', amountCents: 74000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: 25 },
      { id: 'demo-inv-8', invoiceNumber: 'INV-3250', customerName: 'Pixel Forge', amountCents: 61000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: 50 },
      { id: 'demo-inv-9', invoiceNumber: 'INV-3267', customerName: 'Acme GmbH', amountCents: 32000, paidCents: 0, currency: 'USD', status: 'sent' as const, dueOffset: null },
      { id: 'demo-inv-10', invoiceNumber: 'INV-3271', customerName: 'Orbit Logistics', amountCents: 87055, paidCents: 0, currency: 'EUR', status: 'sent' as const, dueOffset: 2 },
      { id: 'demo-inv-11', invoiceNumber: 'INV-3280', customerName: 'Mira Boutique', amountCents: 42500, paidCents: 0, currency: 'GBP', status: 'sent' as const, dueOffset: 13 },
    ]
    const open = records.map((record) => ({ ...record, balanceCents: Math.max(0, record.amountCents - record.paidCents) }))
      .filter((record) => record.balanceCents > 0)
    const usd = open.filter((record) => record.currency === CURRENCY)
    const overdue = usd.filter((record) => record.dueOffset !== null && record.dueOffset < 0)
    const dueBy = (days: number) => usd.filter((record) => record.dueOffset !== null && record.dueOffset >= 0 && record.dueOffset < days)
    const sum = (rows: typeof usd) => rows.reduce((total, record) => total + record.balanceCents, 0)
    const count = (rows: typeof usd) => rows.length
    const formatDate = (offset: number) => new Date(todayMs + offset * DAY_MS).toISOString().slice(0, 10)
    const aging = [
      { key: '1_7_days' as const, rows: overdue.filter((record) => -record.dueOffset! <= 7) },
      { key: '8_30_days' as const, rows: overdue.filter((record) => -record.dueOffset! >= 8 && -record.dueOffset! <= 30) },
      { key: '31_plus_days' as const, rows: overdue.filter((record) => -record.dueOffset! > 30) },
    ]

    const weeklySchedule = Array.from({ length: 13 }, (_, index) => {
      const from = index * 7
      const to = Math.min((index + 1) * 7, 90)
      const rows = usd.filter((record) => record.dueOffset !== null && record.dueOffset >= from && record.dueOffset < to)
      return {
        from: formatDate(from),
        toExclusive: formatDate(to),
        amount: centsToMoney(sum(rows)),
        invoiceCount: count(rows),
      }
    })

    return {
      asOfDateUtc: today,
      openReceivables: centsToMoney(sum(usd)),
      openInvoiceCount: count(usd),
      overdueReceivables: centsToMoney(sum(overdue)),
      overdueInvoiceCount: count(overdue),
      dueWithin7Days: centsToMoney(sum(dueBy(7))),
      dueWithin7DaysCount: count(dueBy(7)),
      dueWithin30Days: centsToMoney(sum(dueBy(30))),
      dueWithin30DaysCount: count(dueBy(30)),
      dueWithin90Days: centsToMoney(sum(dueBy(90))),
      dueWithin90DaysCount: count(dueBy(90)),
      unscheduledReceivables: centsToMoney(sum(usd.filter((record) => record.dueOffset === null))),
      unscheduledInvoiceCount: count(usd.filter((record) => record.dueOffset === null)),
      overdueAging: aging.map(({ key, rows }) => ({ key, amount: centsToMoney(sum(rows)), invoiceCount: count(rows) })),
      weeklySchedule,
      foreignCurrencyExposure: [...new Set(open.filter((record) => record.currency !== CURRENCY).map((record) => record.currency))]
        .map((currency) => {
          const rows = open.filter((record) => record.currency === currency)
          return { currency, openAmount: moneyFromCents(rows.reduce((total, row) => total + row.balanceCents, 0), currency), invoiceCount: rows.length }
        }),
      priorityInvoices: open
        .sort((left, right) => (left.dueOffset ?? Number.MAX_SAFE_INTEGER) - (right.dueOffset ?? Number.MAX_SAFE_INTEGER))
        .slice(0, 12)
        .map((record) => ({
          id: record.id,
          invoiceNumber: record.invoiceNumber,
          customerName: record.customerName,
          balanceDue: moneyFromCents(record.balanceCents, record.currency),
          status: record.status,
          dueDate: record.dueOffset === null ? null : formatDate(record.dueOffset),
          daysOverdue: record.dueOffset !== null && record.dueOffset < 0 ? -record.dueOffset : null,
          dataSource: 'demo',
        })),
    }
  }

const INSIGHT_COPY: Record<RationaleLocale, string> = {
  en: 'In this demo period, {failedCount} of {paymentCount} payments failed ({failedAmount} at risk). The failure rate was {failureRate}; the most common reason was {reason}. These fictional records do not trigger PayPal actions.',
  de: 'In diesem Demo-Zeitraum sind {failedCount} von {paymentCount} Zahlungen fehlgeschlagen ({failedAmount} gefährdet). Die Fehlerquote betrug {failureRate}; der häufigste Grund war {reason}. Diese fiktiven Datensätze lösen keine PayPal-Aktionen aus.',
  tr: 'Bu demo döneminde {paymentCount} ödemenin {failedCount} tanesi başarısız oldu ({failedAmount} risk altında). Başarısızlık oranı {failureRate}; en sık neden {reason} idi. Bu kurmaca kayıtlar PayPal üzerinde işlem başlatmaz.',
  es: 'En este periodo de demostración, fallaron {failedCount} de {paymentCount} pagos ({failedAmount} en riesgo). La tasa de fallos fue {failureRate}; el motivo más frecuente fue {reason}. Estos registros ficticios no activan acciones en PayPal.',
}

const FAILURE_LABELS: Record<RationaleLocale, Record<string, string>> = {
  en: {
    card_declined: 'card declined',
    insufficient_funds: 'insufficient funds',
    expired_card: 'expired card',
    account_restricted: 'restricted account',
    other: 'no failures',
  },
  de: {
    card_declined: 'abgelehnte Karte',
    insufficient_funds: 'unzureichende Deckung',
    expired_card: 'abgelaufene Karte',
    account_restricted: 'eingeschränktes Konto',
    other: 'keine fehlgeschlagenen Zahlungen',
  },
  tr: {
    card_declined: 'kartın reddedilmesi',
    insufficient_funds: 'yetersiz bakiye',
    expired_card: 'süresi dolmuş kart',
    account_restricted: 'kısıtlı hesap',
    other: 'başarısız ödeme yok',
  },
  es: {
    card_declined: 'tarjeta rechazada',
    insufficient_funds: 'fondos insuficientes',
    expired_card: 'tarjeta caducada',
    account_restricted: 'cuenta restringida',
    other: 'ningún pago fallido',
  },
}

function percentageChange(current: Money, previous: Money): number | null {
  const previousAmount = Number(previous.amount)
  if (previousAmount === 0) return null
  return Math.round(((Number(current.amount) - previousAmount) / previousAmount) * 1000) / 10
}

/** Builds the same insight contract as the API, using only the explicitly fictional demo dataset. */
export function buildMockRevenueInsight(days: number): RevenueInsight {
  const to = startOfUtcDay(Date.now()) + DAY_MS
  const from = to - days * DAY_MS
  const range = { from: new Date(from).toISOString(), to: new Date(to).toISOString() }
  const previousRange = { from: new Date(from - days * DAY_MS).toISOString(), to: range.from }
  const current = buildRevenueSummary(range)
  const previous = buildRevenueSummary(previousRange)
  const recovery = buildRecoverySummary(range)
  const failedPayments = getMockPayments().filter(
    (payment) => payment.status === 'failed' && inRange(Date.parse(payment.createdAt), range),
  )

  const reasons = new Map<string, { count: number; cents: number }>()
  for (const payment of failedPayments) {
    if (!payment.failureCode) continue
    const reason = reasons.get(payment.failureCode) ?? { count: 0, cents: 0 }
    reason.count++
    reason.cents += payment.cents
    reasons.set(payment.failureCode, reason)
  }

  const topFailureReasons = [...reasons.entries()]
    .sort((left, right) => right[1].count - left[1].count)
    .slice(0, 3)
    .map(([code, value]) => ({ code, count: value.count, amount: centsToMoney(value.cents) }))

  const metrics = {
    revenue: current.totalRevenue,
    failedAmount: current.failedAmount,
    pendingAmount: current.pendingAmount,
    paymentCount: current.paymentCount,
    failedCount: current.failedCount,
    failureRate: current.paymentCount > 0 ? current.failedCount / current.paymentCount : 0,
    lostAmount: recovery.lostAmount,
    recoveredAmount: recovery.recoveredAmount,
    recoveryRate: recovery.recoveryRate,
    overdueInvoiceCount: 0,
    overdueInvoiceAmount: centsToMoney(0),
    topFailureReasons,
    previous: {
      revenue: previous.totalRevenue,
      failedAmount: previous.failedAmount,
      revenueChangePercent: percentageChange(current.totalRevenue, previous.totalRevenue),
      failedAmountChangePercent: percentageChange(current.failedAmount, previous.failedAmount),
    },
  }

  const locale = getCurrentLocale() as RationaleLocale
  const intlLocale = { en: 'en-US', de: 'de-DE', tr: 'tr-TR', es: 'es-ES' }[locale] ?? 'en-US'
  const reason = topFailureReasons[0]?.code ?? 'other'
  const commentary = (INSIGHT_COPY[locale] ?? INSIGHT_COPY.en)
    .replace('{failedCount}', String(metrics.failedCount))
    .replace('{paymentCount}', String(metrics.paymentCount))
    .replace('{failedAmount}', formatMoney(metrics.failedAmount, intlLocale))
    .replace('{failureRate}', formatPercent(metrics.failureRate, intlLocale))
    .replace('{reason}', FAILURE_LABELS[locale][reason] ?? FAILURE_LABELS[locale].other ?? 'unknown')

  return {
    id: `demo-insight-${Date.now()}`,
    periodFrom: range.from,
    periodTo: range.to,
    metrics,
    commentary,
    commentaryLanguage: locale,
    commentarySource: 'rule',
    gatewayRequestId: null,
    createdAt: new Date().toISOString(),
  }
}

// ---------------------------------------------------------------------------
// Recovery
// ---------------------------------------------------------------------------

type RationaleLocale = 'en' | 'de' | 'tr' | 'es'

// The real backend writes this text (see docs/API_NEEDS.md, "AI text language") and honours Accept-Language.
// The mock mimics that so the UI can be checked in every language.
const SUGGESTION_BY_FAILURE: Record<string, { type: RecoverySuggestionType; rationale: Record<RationaleLocale, string> }> = {
  card_declined: {
    type: 'retry_payment',
    rationale: {
      en: 'The issuer declined the card. Suggest checking the payment details before trying again.',
      de: 'Der Kartenaussteller hat die Karte abgelehnt. Prüfe die Zahlungsdaten, bevor du es erneut versuchst.',
      tr: 'Kartı çıkaran banka kartı reddetti. Yeniden denemeden önce ödeme bilgilerini kontrol etmeyi öneriyoruz.',
      es: 'El emisor rechazó la tarjeta. Se sugiere revisar los datos de pago antes de volver a intentarlo.',
    },
  },
  insufficient_funds: {
    type: 'retry_payment',
    rationale: {
      en: 'Insufficient funds at the time of payment. Suggest retrying in three days.',
      de: 'Zum Zahlungszeitpunkt war das Guthaben nicht ausreichend. Vorschlag: in drei Tagen erneut versuchen.',
      tr: 'Ödeme anında bakiye yetersizdi. Üç gün sonra yeniden denemeyi öneriyoruz.',
      es: 'Fondos insuficientes en el momento del pago. Se sugiere reintentar en tres días.',
    },
  },
  expired_card: {
    type: 'offer_alternative_method',
    rationale: {
      en: 'The saved card has expired. Suggest asking the customer to update it or choose another payment method.',
      de: 'Die gespeicherte Karte ist abgelaufen. Bitte den Kunden, sie zu aktualisieren oder eine andere Zahlungsmethode zu wählen.',
      tr: 'Kayıtlı kartın süresi dolmuş. Müşteriden kartı güncellemesini veya başka bir ödeme yöntemi seçmesini isteyin.',
      es: 'La tarjeta guardada ha caducado. Se sugiere pedir al cliente que la actualice o elija otro método de pago.',
    },
  },
  account_restricted: {
    type: 'offer_alternative_method',
    rationale: {
      en: 'The PayPal account is restricted. Suggest offering an alternative payment method.',
      de: 'Das PayPal-Konto ist eingeschränkt. Vorschlag: eine andere Zahlungsart anbieten.',
      tr: 'PayPal hesabı kısıtlı. Başka bir ödeme yöntemi sunmayı öneriyoruz.',
      es: 'La cuenta de PayPal está restringida. Se sugiere ofrecer un método de pago alternativo.',
    },
  },
}

/** Which failure each suggestion came from, so its text can be produced in the active language. */
const suggestionFailure = new Map<string, string>()

export function localizeSuggestion(s: RecoverySuggestion): RecoverySuggestion {
  const code = suggestionFailure.get(s.id)
  const plan = code ? SUGGESTION_BY_FAILURE[code] : undefined
  if (!plan) return s
  const locale = getCurrentLocale() as RationaleLocale
  const language: RationaleLocale = locale in plan.rationale ? locale : 'en'
  return { ...s, rationale: plan.rationale[language], rationaleLanguage: language }
}

let suggestionStore: Map<string, RecoverySuggestion> | null = null

export function getSuggestionStore(): Map<string, RecoverySuggestion> {
  if (suggestionStore) return suggestionStore

  const failed = getMockPayments()
    .filter((p) => p.status === 'failed' && p.failureCode)
    .slice(0, 14)

  suggestionStore = new Map(
    failed.map((p, index): [string, RecoverySuggestion] => {
      const plan = SUGGESTION_BY_FAILURE[p.failureCode as string] as { type: RecoverySuggestionType; rationale: Record<RationaleLocale, string> }
      const suggestion: RecoverySuggestion = {
        id: `SUG-${String(index + 1).padStart(3, '0')}`,
        issueId: `issue_${p.id}`,
        issueType: 'failed_payment',
        paymentId: p.id,
        invoiceId: null,
        customerName: p.customerName,
        type: plan.type,
        rationale: plan.rationale.en,
        rationaleLanguage: 'en',
        expectedRecovery: p.amount,
        status: 'proposed',
        executable: false,
        executionError: null,
        dataSource: 'demo',
        createdAt: p.updatedAt,
        updatedAt: p.updatedAt,
      }
      suggestionFailure.set(suggestion.id, p.failureCode as string)
      return [suggestion.id, suggestion]
    }),
  )
  return suggestionStore
}

/** Mock of POST /revenue/suggestions/generate: proposes actions for failed payments that have none yet. */
export function generateMockSuggestions(limit = 3): number {
  const store = getSuggestionStore()
  const covered = new Set([...store.values()].map((s) => s.paymentId))
  const fresh = getMockPayments()
    .filter((p) => p.status === 'failed' && p.failureCode && !covered.has(p.id))
    .slice(0, limit)

  for (const p of fresh) {
    const plan = SUGGESTION_BY_FAILURE[p.failureCode as string]
    if (!plan) continue
    const now = new Date().toISOString()
    const id = `SUG-${String(store.size + 1).padStart(3, '0')}`
    store.set(id, {
      id,
      issueId: `issue_${p.id}`,
      issueType: 'failed_payment',
      paymentId: p.id,
      invoiceId: null,
      customerName: p.customerName,
      type: plan.type,
      rationale: plan.rationale.en,
      rationaleLanguage: 'en',
      expectedRecovery: p.amount,
      status: 'proposed',
      executable: false,
      executionError: null,
      dataSource: 'demo',
      createdAt: now,
      updatedAt: now,
    })
    suggestionFailure.set(id, p.failureCode as string)
  }
  return fresh.length
}

export function buildRecoverySummary(range: DateRange): RecoverySummary {
  const summary = buildRevenueSummary(range)
  const lost = Number(summary.failedAmount.amount)
  const recovered = Number(summary.recoveredRevenue.amount)

  return {
    range,
    lostAmount: summary.failedAmount,
    recoveredAmount: summary.recoveredRevenue,
    // Contract definition: recoveredAmount / lostAmount, 0 when nothing was lost.
    recoveryRate: lost > 0 ? Math.min(1, recovered / lost) : 0,
    openSuggestionCount: [...getSuggestionStore().values()].filter((s) => s.status === 'proposed').length,
  }
}

// ---------------------------------------------------------------------------
// AI cost (all numbers and prices are fictional)
// ---------------------------------------------------------------------------

interface ModelProfile {
  key: string
  provider: 'openai' | 'google'
  share: number
  /** Fictional price per 1M tokens. */
  inPerM: number
  outPerM: number
}

const MODELS: readonly ModelProfile[] = [
  { key: 'gpt-4o-mini', provider: 'openai', share: 0.42, inPerM: 0.15, outPerM: 0.6 },
  { key: 'gpt-4o', provider: 'openai', share: 0.12, inPerM: 2.5, outPerM: 10 },
  { key: 'gemini-flash', provider: 'google', share: 0.15, inPerM: 0.1, outPerM: 0.4 },
]

const PROVIDER_LABEL: Record<ModelProfile['provider'], string> = {
  openai: 'OpenAI',
  google: 'Google',
}

interface ModelDayStat {
  model: ModelProfile
  requests: number
  cacheHits: number
  /** Billed tokens (cached requests consume none). */
  inputTokens: number
  outputTokens: number
  cost: number
  /** Cost avoided by cache hits. */
  cacheAvoided: number
}

const dayCache = new Map<number, ModelDayStat[]>()

function modelStatsForDay(day: number): ModelDayStat[] {
  const cached = dayCache.get(day)
  if (cached) return cached

  const rnd = mulberry32(Math.floor(day / DAY_MS))
  const weekday = new Date(day).getUTCDay()
  const weekendFactor = weekday === 0 || weekday === 6 ? 0.6 : 1
  const totalRequests = (9000 + rnd() * 9000) * weekendFactor

  const stats = MODELS.map((model): ModelDayStat => {
    const requests = Math.round(totalRequests * model.share * (0.85 + rnd() * 0.3))
    const hitRate = 0.24 + rnd() * 0.14
    const cacheHits = Math.round(requests * hitRate)
    const billed = requests - cacheHits
    const inputTokens = Math.round(billed * (700 + rnd() * 500))
    const outputTokens = Math.round(billed * (180 + rnd() * 160))
    const cost = (inputTokens * model.inPerM + outputTokens * model.outPerM) / 1_000_000
    const perRequest = billed > 0 ? cost / billed : 0
    return { model, requests, cacheHits, inputTokens, outputTokens, cost, cacheAvoided: perRequest * cacheHits }
  })

  dayCache.set(day, stats)
  return stats
}

const ROUTING_SAVING_RATIO = 0.1 // fictional

export function buildModelCatalog(): AiModelInfo[] {
  return MODELS.map((m) => ({
    key: m.key,
    modelId: m.key,
    label: m.key,
    provider: PROVIDER_LABEL[m.provider],
    inputPricePerMillionTokens: { amount: m.inPerM.toFixed(2), currency: CURRENCY },
    outputPricePerMillionTokens: { amount: m.outPerM.toFixed(2), currency: CURRENCY },
    baseline: m.key === 'gpt-4o',
  }))
}

/** Mock of the single monthly budget record. Settings edit it; the AI Cost page reads it. */
let mockBudget: AiBudgetSetting | null = { limit: usdToMoney(1500), period: 'monthly' }
export const getMockBudget = () => mockBudget
export const setMockBudget = (value: AiBudgetSetting | null) => {
  mockBudget = value
}

export function buildAiUsageSummary(range: DateRange): AiUsageSummary {
  let cost = 0
  let requests = 0
  let hits = 0
  let input = 0
  let output = 0
  let avoided = 0

  for (const day of eachDay(range)) {
    for (const s of modelStatsForDay(day)) {
      cost += s.cost
      requests += s.requests
      hits += s.cacheHits
      input += s.inputTokens
      output += s.outputTokens
      avoided += s.cacheAvoided + s.cost * ROUTING_SAVING_RATIO
    }
  }

  const now = Date.now()
  const monthStart = Date.UTC(new Date(now).getUTCFullYear(), new Date(now).getUTCMonth(), 1)
  let spent = 0
  for (let d = monthStart; d <= startOfUtcDay(now); d += DAY_MS) {
    for (const s of modelStatsForDay(d)) spent += s.cost
  }

  return {
    range,
    totalCost: usdToMoney(cost),
    totalRequests: requests,
    inputTokens: input,
    outputTokens: output,
    cacheHitRate: requests > 0 ? hits / requests : 0,
    savings: usdToMoney(avoided),
    budget: mockBudget ? { limit: mockBudget.limit, spent: usdToMoney(spent), period: 'monthly' } : null,
  }
}

export function buildAiSeries(range: DateRange): AiCostPoint[] {
  return eachDay(range).map((day) => {
    const stats = modelStatsForDay(day)
    return {
      date: new Date(day).toISOString(),
      cost: usdToMoney(stats.reduce((sum, s) => sum + s.cost, 0)),
      requests: stats.reduce((sum, s) => sum + s.requests, 0),
    }
  })
}

export function buildAiBreakdown(range: DateRange): AiCostBreakdown {
  const byModel = new Map<string, { label: string; cost: number; requests: number; input: number; output: number }>()
  const byProvider = new Map<string, { label: string; cost: number; requests: number; input: number; output: number }>()

  const add = (
    map: typeof byModel,
    key: string,
    label: string,
    s: ModelDayStat,
  ) => {
    const row = map.get(key) ?? { label, cost: 0, requests: 0, input: 0, output: 0 }
    row.cost += s.cost
    row.requests += s.requests
    row.input += s.inputTokens
    row.output += s.outputTokens
    map.set(key, row)
  }

  for (const day of eachDay(range)) {
    for (const s of modelStatsForDay(day)) {
      add(byModel, s.model.key, s.model.key, s)
      add(byProvider, s.model.provider, PROVIDER_LABEL[s.model.provider], s)
    }
  }

  const toItems = (map: typeof byModel): AiCostBreakdownItem[] =>
    [...map.entries()]
      .map(([key, v]) => ({
        key,
        label: v.label,
        cost: usdToMoney(v.cost),
        requests: v.requests,
        inputTokens: v.input,
        outputTokens: v.output,
      }))
      .sort((a, b) => Number(b.cost.amount) - Number(a.cost.amount))

  return { byModel: toItems(byModel), byProvider: toItems(byProvider) }
}
