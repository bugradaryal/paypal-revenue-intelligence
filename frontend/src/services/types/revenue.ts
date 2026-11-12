import type { AppLanguage, DataSource, DateRange, IsoDate, IsoDateTime, Money, PageRequest, SortDirection } from './common'

/** Shapes from docs/API_CONTRACT.md v0.8. */

// ---- Payments -------------------------------------------------------------

export type PaymentStatus = 'completed' | 'pending' | 'failed' | 'refunded'

export interface Payment {
  id: string
  customerId: string
  customerName: string
  amount: Money
  status: PaymentStatus
  /** Stable machine code, translated by the UI. Only set when status is "failed"; may be a value the UI does not know yet. */
  failureCode: string | null
  paypalTransactionId: string | null
  dataSource: DataSource
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface PaymentDetail extends Payment {
  statusHistory: { status: string; occurredAt: IsoDateTime }[]
}

export interface PaymentQuery extends PageRequest {
  status?: PaymentStatus[]
  search?: string
  customerId?: string
  range?: DateRange
  sortBy?: 'createdAt' | 'amount'
  sortDir?: SortDirection
}

// ---- Overview -------------------------------------------------------------

export interface RevenuePoint {
  /** Start of the bucket (one day). */
  date: IsoDateTime
  revenue: Money
  recovered: Money
}

export interface RevenueSummary {
  range: DateRange
  totalRevenue: Money
  failedAmount: Money
  pendingAmount: Money
  recoveredRevenue: Money
  paymentCount: number
  failedCount: number
  pendingCount: number
  /** Payments not in USD: listed, but not part of the totals. */
  excludedPaymentCount: number
  series: RevenuePoint[]
}

// ---- Recovery -------------------------------------------------------------

export interface RecoverySummary {
  range: DateRange
  lostAmount: Money
  recoveredAmount: Money
  /** 0..1, calculated by the backend. */
  recoveryRate: number
  openSuggestionCount: number
}

export type RevenueIssueStatus = 'open' | 'recovered' | 'void'
export type RevenueIssueType = 'failed_payment' | 'pending_payment' | 'overdue_invoice'
export type RecoveryEvidence = 'status_changed' | 'linked_invoice_payment' | 'matching_retry_payment'

export interface RevenueIssue {
  id: string
  type: RevenueIssueType
  status: RevenueIssueStatus
  paymentId: string | null
  invoiceId: string | null
  customerId: string | null
  customerName: string | null
  amount: Money
  recoveredAmount: Money
  failureCode: string | null
  dataSource: DataSource
  problemSince: IsoDateTime
  detectedAt: IsoDateTime
  resolvedAt: IsoDateTime | null
  evidence: RecoveryEvidence | null
}

export interface IssueQuery extends PageRequest {
  status?: RevenueIssueStatus[]
  type?: RevenueIssueType[]
  sortBy?: 'problemSince' | 'amount'
  sortDir?: SortDirection
}

export type RecoverySuggestionType = 'send_reminder' | 'retry_payment' | 'offer_alternative_method'
export type RecoverySuggestionStatus = 'proposed' | 'approved' | 'rejected' | 'executed' | 'failed'

/** An AI-proposed external action. Nothing runs until the user approves it. */
export interface RecoverySuggestion {
  id: string
  issueId: string
  issueType: RevenueIssueType
  paymentId: string | null
  invoiceId: string | null
  customerName: string | null
  type: RecoverySuggestionType
  /** Human-readable explanation written by the AI. */
  rationale: string
  /** Language the rationale was generated in (the Accept-Language of the request, or "en"). */
  rationaleLanguage: AppLanguage
  expectedRecovery: Money
  status: RecoverySuggestionStatus
  /** false: advice only. Even when approved, no automatic action runs. */
  executable: boolean
  executionError: string | null
  dataSource: DataSource
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
}

export interface CashFlowInvoice {
  id: string
  invoiceNumber: string | null
  customerName: string | null
  balanceDue: Money
  status: 'draft' | 'sent' | 'payment_pending' | 'partially_paid' | 'paid' | 'cancelled' | 'refunded'
  dueDate: IsoDate | null
  daysOverdue: number | null
  dataSource: DataSource
}

export interface CashFlowOutlook {
  asOfDateUtc: IsoDate
  openReceivables: Money
  openInvoiceCount: number
  overdueReceivables: Money
  overdueInvoiceCount: number
  dueWithin7Days: Money
  dueWithin7DaysCount: number
  dueWithin30Days: Money
  dueWithin30DaysCount: number
  dueWithin90Days: Money
  dueWithin90DaysCount: number
  unscheduledReceivables: Money
  unscheduledInvoiceCount: number
  overdueAging: { key: '1_7_days' | '8_30_days' | '31_plus_days'; amount: Money; invoiceCount: number }[]
  weeklySchedule: { from: IsoDate; toExclusive: IsoDate; amount: Money; invoiceCount: number }[]
  foreignCurrencyExposure: { currency: string; openAmount: Money; invoiceCount: number }[]
  priorityInvoices: CashFlowInvoice[]
}

export interface RevenueAnalytics {
  range: DateRange
  asOfDateUtc: IsoDate
  leakage: RevenueLeakage
  daily: RevenueAnalyticsDay[]
  failureReasons: FailureReasonAnalysis[]
  failureTrend: FailureTrendPoint[]
  cohorts: RevenueCohort[]
  anomalies: RevenueAnomalyReport
  excludedPaymentCount: number
}

export interface RevenueLeakage {
  transactionActivity: Money
  completedAmount: Money
  completedCount: number
  failedAttemptAmount: Money
  failedCount: number
  pendingAmount: Money
  pendingCount: number
  refundedAmount: Money
  refundedCount: number
  currentOpenReceivables: Money
  currentOpenInvoiceCount: number
  currentOverdueReceivables: Money
  currentOverdueInvoiceCount: number
  cancellationTrackingAvailable: boolean
}

export interface RevenueAnalyticsDay {
  date: IsoDate
  completedAmount: Money
  failedAmount: Money
  pendingAmount: Money
  refundedAmount: Money
}

export interface FailureReasonAnalysis {
  code: string
  count: number
  amountUsd: Money
}

export interface FailureTrendPoint {
  date: IsoDate
  code: string
  count: number
}

export interface RevenueCohort {
  cohortMonth: IsoDate
  cohortSize: number
  periods: RevenueCohortPeriod[]
}

export interface RevenueCohortPeriod {
  ageInMonths: number
  activeCustomers: number
  retentionRate: number
  revenuePerCustomer: Money
}

export interface RevenueAnomalyReport {
  windowDays: number
  failureRateSpike: {
    currentRate: number
    baselineRate: number
    currentFailedCount: number
    currentPaymentCount: number
    baselinePaymentCount: number
  } | null
  revenueDrops: {
    currency: string
    currentRevenue: Money
    baselineRevenue: Money
    changePercent: number
    currentPaymentCount: number
    baselinePaymentCount: number
  }[]
}

export interface SuggestionQuery extends PageRequest {
  status?: RecoverySuggestionStatus[]
  paymentId?: string
  sortBy?: 'createdAt' | 'expectedRecovery'
  sortDir?: SortDirection
}

export interface GenerateSuggestionsResult {
  createdCount: number
}

export interface DecideSuggestionRequest {
  decision: 'approve' | 'reject'
}
