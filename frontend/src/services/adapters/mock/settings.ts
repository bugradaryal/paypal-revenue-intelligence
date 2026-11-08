import { ApiError } from '../../errors'
import type { AiBudgetSetting, CacheStats, UpdateBudgetRequest } from '../../types'
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

  getCacheStats: (): CacheStats => ({ entryCount: cacheEntries }),

  clearCache: () => {
    const removedCount = cacheEntries
    cacheEntries = 0
    return { removedCount }
  },
}
