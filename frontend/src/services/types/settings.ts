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
  modelId: string
  label: string
  provider: string
  inputPricePerMillionTokens: Money
  outputPricePerMillionTokens: Money
  /** The model savings are measured against. */
  baseline: boolean
}

export interface CacheStats {
  enabled: boolean
  entries: number
  activeEntries: number
  hits: number
  misses: number
  hitRate: number
}

export interface ClearCacheResult {
  deletedCount: number
}

export type PayPalEnvironment = 'sandbox' | 'live'
export type PayPalSyncStatus = 'running' | 'succeeded' | 'failed'

export interface PayPalSyncRun {
  id: string
  status: PayPalSyncStatus
  startedAt: string
  finishedAt: string | null
  rangeFrom: string
  rangeTo: string
  importedPaymentCount: number
  importedInvoiceCount: number
  skippedCount: number
  invoiceImportSkipped: boolean
  error: string | null
}

export interface PayPalConnection {
  configured: boolean
  webhookIdConfigured: boolean
  connected: boolean
  environment: PayPalEnvironment
  apiAccessStatus: 'available' | 'denied' | 'not_configured'
  lastSuccessfulSyncAt: string | null
  latestSync: PayPalSyncRun | null
  error: string | null
}

export interface PayPalTestConnectionResult {
  connected: boolean
  environment: PayPalEnvironment
  testedAt: string
  error: string | null
}

export interface SavePayPalCredentialsRequest {
  clientId: string
  clientSecret: string
  webhookId: string
}
