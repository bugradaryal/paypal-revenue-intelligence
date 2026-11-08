import { ContractNotReadyError } from '../../errors'
import { http } from '../../http'
import type {
  AiBudgetSetting,
  AiCostBreakdown,
  AiModelInfo,
  CacheStats,
  ClearCacheResult,
  AiCostPoint,
  AiUsageSummary,
  DateRange,
  GenerateSuggestionsResult,
  Page,
  Payment,
  PaymentDetail,
  RecoverySuggestion,
  RecoverySummary,
  RevenueSummary,
} from '../../types'
import type { Api } from '../types'

/**
 * Real adapter: one function per operation, URLs and shapes straight from docs/API_CONTRACT.md (v0.3).
 * Which operations are actually live is decided in ../index.ts (READY); the rest stay on the mock adapter.
 */

/** AI generation endpoints can take up to 60 s (contract, general rule 13). */
const AI_TIMEOUT_MS = 60_000

const range = (r: DateRange) => ({ from: r.from, to: r.to })

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
      http.post<GenerateSuggestionsResult>('revenue/suggestions/generate', { signal: o?.signal, timeoutMs: AI_TIMEOUT_MS }),
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
    getBudget: (o) => http.get<AiBudgetSetting | null>('gateway/budgets', { signal: o?.signal }),
    setBudget: (request, o) => http.put<AiBudgetSetting>('gateway/budgets', { body: request, signal: o?.signal }),
    listModels: (o) => http.get<AiModelInfo[]>('gateway/models', { signal: o?.signal }),
    getCacheStats: (o) => http.get<CacheStats>('gateway/cache/stats', { signal: o?.signal }),
    clearCache: (o) => http.delete<ClearCacheResult>('gateway/cache', { signal: o?.signal }),
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
