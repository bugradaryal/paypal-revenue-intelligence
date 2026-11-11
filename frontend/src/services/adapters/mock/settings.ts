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

  getPayPalConnection: (): PayPalConnection => ({
    configured: false,
    connected: false,
    environment: 'sandbox',
    apiAuthenticationStatus: 'not_configured',
    lastSuccessfulSyncAt: null,
    latestSync: null,
    error: null,
  }),

  testPayPalConnection: (): PayPalTestConnectionResult => ({
    connected: false,
    environment: 'sandbox',
    testedAt: new Date().toISOString(),
    error: 'PayPal connection tests require real data mode.',
  }),

  savePayPalCredentials: (_request: SavePayPalCredentialsRequest): { configured: boolean } => {
    throw new ApiError({
      status: 400,
      title: 'Saving PayPal credentials is unavailable in mock mode.',
      code: 'mock_paypal_credentials',
    })
  },

  syncPayPalTransactions: (_days: number): PayPalSyncRun => {
    throw new ApiError({
      status: 400,
      title: 'PayPal sync is unavailable in mock mode.',
      code: 'mock_paypal_sync',
    })
  },
}
