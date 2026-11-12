import { ApiError } from '../../errors'
import type { InsightQuery, IssueQuery, Page, RequestOptions, RevenueInsight } from '../../types'
import type { Api } from '../types'
import {
  buildAiBreakdown,
  buildAiSeries,
  buildAiUsageSummary,
  buildMockCashFlowOutlook,
  buildMockRevenueAnalytics,
  buildMockRevenueInsight,
  buildRecoverySummary,
  buildRevenueSummary,
  generateMockSuggestions,
  getMockPayments,
  getMockIssues,
  getSuggestionStore,
  localizeSuggestion,
  toPayment,
  toPaymentDetail,
} from './data'
import { isMockFailing } from './failure'
import { mockSettings } from './settings'
import { mockStreamChat } from './workspace'

/** Local demo adapter; see data.ts for the dataset's scope. */
const mockInsights: RevenueInsight[] = []

export { isMockFailing, setMockFailure } from './failure'

function delay(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) return reject(new DOMException('Aborted', 'AbortError'))
    const onAbort = () => {
      clearTimeout(timer)
      reject(new DOMException('Aborted', 'AbortError'))
    }
    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort)
      resolve()
    }, ms)
    signal?.addEventListener('abort', onAbort, { once: true })
  })
}

/** Simulates network latency and failures, and returns a copy so callers cannot mutate the mock store. */
async function respond<T>(compute: () => T, options?: RequestOptions): Promise<T> {
  await delay(250 + Math.random() * 300, options?.signal)
  if (isMockFailing()) throw new ApiError({ status: 503, title: 'Simulated backend failure (mock)', code: 'mock_failure' })
  return structuredClone(compute())
}

function paginate<T>(all: T[], page: number, pageSize: number): Page<T> {
  const start = (page - 1) * pageSize
  return { items: all.slice(start, start + pageSize), page, pageSize, totalItems: all.length }
}

export const mockApi: Api = {
  revenue: {
    getSummary: (range, o) => respond(() => buildRevenueSummary(range), o),

    listPayments: (q, o) =>
      respond(() => {
        const from = q.range ? Date.parse(q.range.from) : -Infinity
        const to = q.range ? Date.parse(q.range.to) : Infinity
        const needle = q.search?.trim().toLowerCase()

        const rows = getMockPayments().filter((p) => {
          const created = Date.parse(p.createdAt)
          if (created < from || created >= to) return false
          if (q.status?.length && !q.status.includes(p.status)) return false
          if (needle && !p.customerName.toLowerCase().includes(needle) && !p.id.toLowerCase().includes(needle)) return false
          return true
        })

        const dir = q.sortDir === 'asc' ? 1 : -1
        rows.sort((a, b) =>
          q.sortBy === 'amount' ? dir * (a.cents - b.cents) : dir * (Date.parse(a.createdAt) - Date.parse(b.createdAt)),
        )

        const page = paginate(rows, q.page, q.pageSize)
        return { ...page, items: page.items.map(toPayment) }
      }, o),

    getPayment: (id, o) =>
      respond(() => {
        const found = getMockPayments().find((p) => p.id === id)
        if (!found) throw new ApiError({ status: 404, title: 'Payment not found', code: 'payment_not_found' })
        return toPaymentDetail(found)
      }, o),

    getRecoverySummary: (range, o) => respond(() => buildRecoverySummary(range), o),

    getCashFlowOutlook: (o) => respond(buildMockCashFlowOutlook, o),

    getRevenueAnalytics: (range, o) => respond(() => buildMockRevenueAnalytics(range), o),

    listIssues: (q: IssueQuery, o) =>
      respond(() => {
        const dir = q.sortDir === 'asc' ? 1 : -1
        const rows = getMockIssues()
          .filter((issue) => !q.status?.length || q.status.includes(issue.status))
          .filter((issue) => !q.type?.length || q.type.includes(issue.type))
        rows.sort((a, b) =>
          q.sortBy === 'amount'
            ? dir * (Number(a.amount.amount) - Number(b.amount.amount))
            : dir * (Date.parse(a.problemSince) - Date.parse(b.problemSince)),
        )
        return paginate(rows, q.page, q.pageSize)
      }, o),

    listSuggestions: (q, o) =>
      respond(() => {
        const dir = q.sortDir === 'asc' ? 1 : -1
        const rows = [...getSuggestionStore().values()]
          .filter((s) => !q.status?.length || q.status.includes(s.status))
          .filter((s) => !q.paymentId || s.paymentId === q.paymentId)
        rows.sort((a, b) =>
          q.sortBy === 'expectedRecovery'
            ? dir * (Number(a.expectedRecovery.amount) - Number(b.expectedRecovery.amount))
            : dir * (Date.parse(a.createdAt) - Date.parse(b.createdAt)),
        )
        const page = paginate(rows, q.page, q.pageSize)
        return { ...page, items: page.items.map(localizeSuggestion) }
      }, o),

    decideSuggestion: (id, request, o) =>
      respond(() => {
        const store = getSuggestionStore()
        const current = store.get(id)
        if (!current) throw new ApiError({ status: 404, title: 'Suggestion not found', code: 'suggestion_not_found' })
        if (current.status !== 'proposed') {
          throw new ApiError({ status: 409, title: 'Suggestion was already decided', code: 'suggestion_already_decided' })
        }
        const updated = {
          ...current,
          status: request.decision === 'approve' ? ('approved' as const) : ('rejected' as const),
          updatedAt: new Date().toISOString(),
        }
        store.set(id, updated)
        return localizeSuggestion(updated)
      }, o),

    generateSuggestions: async (o) => {
      await delay(1500, o?.signal) // AI generation is slow
      return respond(() => ({ createdCount: generateMockSuggestions() }), o)
    },

    listInsights: (q: InsightQuery, o) =>
      respond(() => {
        if (mockInsights.length === 0) mockInsights.push(buildMockRevenueInsight(30))
        return paginate([...mockInsights].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)), q.page, q.pageSize)
      }, o),

    generateInsight: async (days, o) => {
      await delay(700, o?.signal)
      return respond(() => {
        if (!Number.isInteger(days) || days < 1 || days > 90) {
          throw new ApiError({ status: 400, title: 'Insight period must be between 1 and 90 days', code: 'validation_failed' })
        }
        const insight = buildMockRevenueInsight(days)
        mockInsights.unshift(insight)
        return insight
      }, o)
    },
  },

  aiCost: {
    getUsageSummary: (range, o) => respond(() => buildAiUsageSummary(range), o),
    getUsageSeries: (range, o) => respond(() => buildAiSeries(range), o),
    getBreakdown: (range, o) => respond(() => buildAiBreakdown(range), o),
  },

  settings: {
    getBudget: (o) => respond(() => mockSettings.getBudget(), o),
    setBudget: (request, o) => respond(() => mockSettings.setBudget(request), o),
    listModels: (o) => respond(() => mockSettings.listModels(), o),
    getCacheStats: (o) => respond(() => mockSettings.getCacheStats(), o),
    clearCache: (o) => respond(() => mockSettings.clearCache(), o),
    getPayPalConnection: (o) => respond(() => mockSettings.getPayPalConnection(), o),
    testPayPalConnection: (o) => respond(() => mockSettings.testPayPalConnection(), o),
    syncPayPalTransactions: (days, o) => respond(() => mockSettings.syncPayPalTransactions(days), o),
    savePayPalCredentials: (request, o) => respond(() => mockSettings.savePayPalCredentials(request), o),
  },

  workspace: {
    streamChat: (request, o) => mockStreamChat(request, o),
  },
}
