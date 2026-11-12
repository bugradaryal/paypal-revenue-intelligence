import { ApiError } from '../../errors'
import type {
  AiBudgetSetting,
  CacheStats,
  PayPalConnection,
  PayPalSyncRun,
  PayPalTestConnectionResult,
  SavePayPalCredentialsRequest,
  UpdateBudgetRequest,
} from '../../types'
import { buildModelCatalog, getMockBudget, setMockBudget } from './data'

/** Mock of the planned settings endpoints. Shapes are PROPOSED (types/settings.ts). */

let cacheEntries = 1284
const initialSyncAt = new Date()
let mockPayPalConnection: PayPalConnection = {
  configured: true,
  webhookIdConfigured: true,
  connected: true,
  environment: 'sandbox',
  apiAccessStatus: 'available',
  lastSuccessfulSyncAt: initialSyncAt.toISOString(),
  latestSync: {
    id: 'demo-paypal-sync-initial',
    status: 'succeeded',
    startedAt: initialSyncAt.toISOString(),
    finishedAt: initialSyncAt.toISOString(),
    rangeFrom: new Date(initialSyncAt.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    rangeTo: initialSyncAt.toISOString(),
    importedPaymentCount: 24,
    importedInvoiceCount: 6,
    skippedCount: 0,
    invoiceImportSkipped: false,
    error: null,
  },
  error: null,
}

export const mockSettings = {
  getBudget: (): AiBudgetSetting | null => getMockBudget(),

  setBudget: (request: UpdateBudgetRequest): AiBudgetSetting => {
    const amount = Number(request.limit.amount)
    if (!Number.isFinite(amount) || amount <= 0) {
      throw new ApiError({
        status: 400,
        title: 'Validation failed',
        code: 'validation_failed',
        fieldErrors: { limit: ['Limit must be greater than zero.'] },
      })
    }
    const next: AiBudgetSetting = { limit: request.limit, period: 'monthly' }
    setMockBudget(next)
    return next
  },

  listModels: () => buildModelCatalog(),

  getCacheStats: (): CacheStats => ({
    enabled: true,
    entries: cacheEntries,
    activeEntries: cacheEntries,
    hits: 845,
    misses: 439,
    hitRate: 845 / 1284,
  }),

  clearCache: () => {
    const removedCount = cacheEntries
    cacheEntries = 0
    return { deletedCount: removedCount }
  },

  getPayPalConnection: (): PayPalConnection => mockPayPalConnection,

  testPayPalConnection: (): PayPalTestConnectionResult => ({
    connected: true,
    environment: 'sandbox',
    testedAt: new Date().toISOString(),
    error: null,
  }),

  savePayPalCredentials: (request: SavePayPalCredentialsRequest): { configured: boolean } => {
    mockPayPalConnection = {
      ...mockPayPalConnection,
      configured: true,
      webhookIdConfigured: request.webhookId.trim()
        ? true
        : mockPayPalConnection.webhookIdConfigured,
    }
    return { configured: true }
  },

  syncPayPalTransactions: (days: number): PayPalSyncRun => {
    if (!Number.isInteger(days) || days < 1 || days > 90) {
      throw new ApiError({
        status: 400,
        title: 'Sync period must be between 1 and 90 days.',
        code: 'validation_failed',
      })
    }

    const now = new Date()
    const timestamp = now.toISOString()
    const run: PayPalSyncRun = {
      id: `demo-paypal-sync-${now.getTime()}`,
      status: 'succeeded',
      startedAt: timestamp,
      finishedAt: timestamp,
      rangeFrom: new Date(now.getTime() - days * 24 * 60 * 60 * 1000).toISOString(),
      rangeTo: timestamp,
      importedPaymentCount: 8,
      importedInvoiceCount: 2,
      skippedCount: 0,
      invoiceImportSkipped: false,
      error: null,
    }
    mockPayPalConnection = {
      ...mockPayPalConnection,
      lastSuccessfulSyncAt: timestamp,
      latestSync: run,
    }
    return run
  },
}
