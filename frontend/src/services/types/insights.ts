import type { AppLanguage, IsoDateTime, Money, PageRequest } from './common'

export interface FailureReason {
  code: string
  count: number
  amount: Money
}

export interface PreviousPeriodMetrics {
  revenue: Money
  failedAmount: Money
  /** Percentage change, or null when the previous period was zero. */
  revenueChangePercent: number | null
  /** Percentage change, or null when the previous period was zero. */
  failedAmountChangePercent: number | null
}

export interface RevenueInsightMetrics {
  revenue: Money
  failedAmount: Money
  pendingAmount: Money
  paymentCount: number
  failedCount: number
  /** 0..1 */
  failureRate: number
  lostAmount: Money
  recoveredAmount: Money
  /** 0..1 */
  recoveryRate: number
  overdueInvoiceCount: number
  overdueInvoiceAmount: Money
  topFailureReasons: FailureReason[]
  previous: PreviousPeriodMetrics
}

export interface RevenueInsight {
  id: string
  periodFrom: IsoDateTime
  /** Exclusive upper bound. */
  periodTo: IsoDateTime
  metrics: RevenueInsightMetrics
  commentary: string
  commentaryLanguage: AppLanguage
  commentarySource: 'ai' | 'rule'
  gatewayRequestId: string | null
  createdAt: IsoDateTime
}

export type InsightQuery = PageRequest
