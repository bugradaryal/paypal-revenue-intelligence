import type { Money } from './common'

/**
 * PROPOSED. The contract lists these endpoints (v0.3, "Gün 4: Ayarlar") but has not defined their
 * schemas yet. Until it does, they are served by the mock adapter. See docs/API_NEEDS.md.
 */

export interface AiBudgetSetting {
  limit: Money
  period: 'monthly'
}

export interface UpdateBudgetRequest {
  limit: Money
}

/** Read-only catalog entry. */
export interface AiModelInfo {
  key: string
  label: string
  provider: string
  inputPricePerMillionTokens: Money
  outputPricePerMillionTokens: Money
  /** The model savings are measured against. */
  baseline: boolean
}

export interface CacheStats {
  entryCount: number
}

export interface ClearCacheResult {
  removedCount: number
}
