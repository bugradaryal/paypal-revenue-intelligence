import type { AppLanguage, DataSource, DateRange, IsoDateTime, Money, PageRequest, SortDirection } from './common'

/** Shapes from docs/API_CONTRACT.md v0.3. */

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

export type RecoverySuggestionType = 'send_reminder' | 'retry_payment' | 'offer_alternative_method'
export type RecoverySuggestionStatus = 'proposed' | 'approved' | 'rejected' | 'executed' | 'failed'

/** An AI-proposed external action. Nothing runs until the user approves it. */
export interface RecoverySuggestion {
  id: string
  paymentId: string
  customerName: string
  type: RecoverySuggestionType
  /** Human-readable explanation written by the AI. */
  rationale: string
  /** Language the rationale was generated in (the Accept-Language of the request, or "en"). */
  rationaleLanguage: AppLanguage
  expectedRecovery: Money
  status: RecoverySuggestionStatus
  /** false: advice only. Even when approved, no automatic action runs. */
  executable: boolean
  dataSource: DataSource
  createdAt: IsoDateTime
  updatedAt: IsoDateTime
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
