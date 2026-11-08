import type { DateRange, IsoDateTime, Money } from './common'

/** Shapes from docs/API_CONTRACT.md v0.3. */

export interface AiBudget {
  limit: Money
  spent: Money
  period: 'monthly'
}

export interface AiUsageSummary {
  range: DateRange
  totalCost: Money
  totalRequests: number
  inputTokens: number
  outputTokens: number
  /** 0..1, measured by the gateway. */
  cacheHitRate: number
  /** Measured by the gateway against a defined baseline. The UI never estimates savings itself. */
  savings: Money
  /** null when no budget is configured. `spent` is the current UTC calendar month, whatever the range. */
  budget: AiBudget | null
}

export interface AiCostPoint {
  /** Start of the bucket (one day). */
  date: IsoDateTime
  cost: Money
  requests: number
}

export interface AiCostBreakdownItem {
  /** Stable identifier, e.g. a model id or provider id. */
  key: string
  label: string
  cost: Money
  requests: number
  inputTokens: number
  outputTokens: number
}

export interface AiCostBreakdown {
  byModel: AiCostBreakdownItem[]
  byProvider: AiCostBreakdownItem[]
}
