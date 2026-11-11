import { ContractNotReadyError } from '../../errors'
import { http } from '../../http'
import type {
  AiBudgetSetting,
  AiCostBreakdown,
  AiModelInfo,
  AiCostPoint,
  AiUsageSummary,
  DateRange,
  GenerateSuggestionsResult,
  Page,
  PayPalConnection,
  PayPalSyncRun,
  PayPalTestConnectionResult,
  Payment,
  PaymentDetail,
  RecoverySuggestion,
  RecoverySummary,
  RevenueSummary,
} from '../../types'
import type { Api } from '../types'

/**
 * Real adapter: one function per operation, URLs and shapes straight from docs/API_CONTRACT.md (v0.4).
 * Which operations are actually live is decided in ../index.ts (READY); the rest stay on the mock adapter.
 */

/** AI generation and PayPal sync/auth checks can take up to 60 seconds. */
const LONG_REQUEST_TIMEOUT_MS = 60_000

const range = (r: DateRange) => ({ from: r.from, to: r.to })

interface ProviderModelResponse {
  id: string
  provider: string
  modelId: string
  displayName: string
  inputPricePerMillion: AiModelInfo['inputPricePerMillionTokens']
  outputPricePerMillion: AiModelInfo['outputPricePerMillionTokens']
  isBaseline: boolean
}

interface ProviderBudgetResponse {
  isConfigured: boolean
  limit: AiBudgetSetting['limit'] | null
}

interface ProviderCacheStatsResponse {
  enabled: boolean
  entries: number
  activeEntries: number
  hits: number
  misses: number
  hitRate: number
}

interface ProviderClearCacheResponse {
  deletedCount: number
}

function toModelInfo(model: ProviderModelResponse): AiModelInfo {
  if (!model.inputPricePerMillion || !model.outputPricePerMillion) {
    throw new Error(`The model catalog returned incomplete pricing for "${model.modelId}".`)
  }

  return {
    key: model.id,
    label: model.displayName,
    provider: model.provider,
    inputPricePerMillionTokens: model.inputPricePerMillion,
    outputPricePerMillionTokens: model.outputPricePerMillion,
    baseline: model.isBaseline,
  }
}

function toBudgetSetting(budget: ProviderBudgetResponse): AiBudgetSetting | null {
  return budget.isConfigured && budget.limit ? { limit: budget.limit, period: 'monthly' } : null
}

export const realApi: Api = {
  revenue: {
    getSummary: (r, o) => http.get<RevenueSummary>('revenue/summary', { query: range(r), signal: o?.signal }),

    listPayments: (q, o) =>
      http.get<Page<Payment>>('revenue/payments', {
        query: {
          page: q.page,
          pageSize: q.pageSize,
          status: q.status,
          search: q.search,
          customerId: q.customerId,
          from: q.range?.from,
          to: q.range?.to,
          sortBy: q.sortBy,
          sortDir: q.sortDir,
        },
        signal: o?.signal,
      }),

    getPayment: (id, o) => http.get<PaymentDetail>(`revenue/payments/${encodeURIComponent(id)}`, { signal: o?.signal }),

    getRecoverySummary: (r, o) =>
      http.get<RecoverySummary>('revenue/recovery/summary', { query: range(r), signal: o?.signal }),

    listSuggestions: (q, o) =>
      http.get<Page<RecoverySuggestion>>('revenue/suggestions', {
        query: {
          page: q.page,
          pageSize: q.pageSize,
          status: q.status,
          paymentId: q.paymentId,
          sortBy: q.sortBy,
          sortDir: q.sortDir,
        },
        signal: o?.signal,
      }),

    decideSuggestion: (id, request, o) =>
      http.post<RecoverySuggestion>(`revenue/suggestions/${encodeURIComponent(id)}/decision`, {
        body: request,
        signal: o?.signal,
      }),

    generateSuggestions: (o) =>
      http.post<GenerateSuggestionsResult>('revenue/suggestions/generate', { signal: o?.signal, timeoutMs: LONG_REQUEST_TIMEOUT_MS }),
  },

  aiCost: {
    getUsageSummary: (r, o) => http.get<AiUsageSummary>('gateway/usage/summary', { query: range(r), signal: o?.signal }),

    // The contract wraps the list as { range, items }; the UI only needs the items.
    getUsageSeries: (r, o) =>
      http
        .get<{ range: DateRange; items: AiCostPoint[] }>('gateway/usage/series', { query: range(r), signal: o?.signal })
        .then((body) => body.items),

    getBreakdown: (r, o) => http.get<AiCostBreakdown>('gateway/usage/breakdown', { query: range(r), signal: o?.signal }),
  },

  // Paths are in the contract ("Gün 4: Ayarlar"); the shapes are still PROPOSED (see types/settings.ts).
  settings: {
    getBudget: (o) =>
      http.get<ProviderBudgetResponse>('gateway/budgets', { signal: o?.signal }).then(toBudgetSetting),
    setBudget: (request, o) =>
      http.put<ProviderBudgetResponse>('gateway/budgets', { body: request, signal: o?.signal }).then((budget) => {
        const setting = toBudgetSetting(budget)
        if (!setting) throw new Error('The budget API did not return the saved budget limit.')
        return setting
      }),
    listModels: (o) =>
      http.get<ProviderModelResponse[]>('gateway/models', { signal: o?.signal }).then((models) => models.map(toModelInfo)),
    getCacheStats: (o) =>
      http.get<ProviderCacheStatsResponse>('gateway/cache/stats', { signal: o?.signal }).then((stats) => ({
        enabled: stats.enabled,
        entries: stats.entries,
        activeEntries: stats.activeEntries,
        hits: stats.hits,
        misses: stats.misses,
        hitRate: stats.hitRate,
      })),
    clearCache: (o) =>
      http.delete<ProviderClearCacheResponse>('gateway/cache', { signal: o?.signal }).then((result) => ({
        deletedCount: result.deletedCount,
      })),
    getPayPalConnection: (o) =>
      http.get<PayPalConnection>('paypal/connection', { signal: o?.signal, timeoutMs: LONG_REQUEST_TIMEOUT_MS }),
    testPayPalConnection: (o) =>
      http.post<PayPalTestConnectionResult>('paypal/test-connection', { signal: o?.signal, timeoutMs: LONG_REQUEST_TIMEOUT_MS }),
    syncPayPalTransactions: (days, o) =>
      http.post<PayPalSyncRun>('paypal/sync', { query: { days }, signal: o?.signal, timeoutMs: LONG_REQUEST_TIMEOUT_MS }),
    savePayPalCredentials: (request, o) =>
      http.put<{ configured: boolean }>('paypal/credentials', { body: request, signal: o?.signal }),
  },

  workspace: {
    // The streaming schema is not defined until contract 0.4, so there is nothing to call yet.
    // adapters/index.ts keeps this operation on the mock until then.
    // eslint-disable-next-line require-yield
    async *streamChat() {
      throw new ContractNotReadyError('workspace.streamChat')
    },
  },
}
