import { http } from '../../http'
import { getCurrentLocale } from '@/i18n/current'
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

interface CompletionResponse {
  content: string
  model: string
  provider: string
  usage: {
    inputTokens: number
    outputTokens: number
  }
  cost: {
    amount: string
    currency: string
  }
  cache: {
    status: string
  }
}

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

const PAYMENT_INTENT = /\b(payment|payments|transaction|transactions|charge|charges|zahlung|zahlungen|transaktion|transaktionen|pago|pagos)\b|ödem|işlem/i
const PAYMENT_QUERY_STOP_WORDS = new Set([
  'a', 'about', 'an', 'and', 'at', 'can', 'could', 'do', 'does', 'failed', 'for', 'give', 'how', 'i',
  'is', 'me', 'my', 'of', 'on', 'or', 'payment', 'payments', 'please', 'recommend', 'recommendation',
  'should', 'suggest', 'suggestion', 'the', 'this', 'to', 'transaction', 'transactions', 'what', 'with',
  'you', 'your', 'zahlung', 'zahlungen', 'transaktion', 'transaktionen', 'pago', 'pagos', 'por', 'para',
  'una', 'un', 'que', 'como', 'meine', 'bitte', 'für', 'von', 'und', 'mit', 'ist', 'was', 'kann',
  'soll', 'mir', 'analysiere', 'analiz', 'ödemesi', 'ödemeye', 'ödemesine', 'ödeme', 'işlem', 'işlemi',
  'için', 'bana', 'öneri', 'öneride', 'bulun', 'hakkında', 'bak', 'şu', 'bu',
])

function paymentSearchTerms(question: string): string[] {
  const paymentId = question.match(/\b[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}\b/i)?.[0]
  if (paymentId) return [paymentId]

  return [...new Set(
    question
      .match(/[\p{L}\p{N}@._-]+/gu)
      ?.filter((word) => word.length >= 3 && !PAYMENT_QUERY_STOP_WORDS.has(word.toLocaleLowerCase())) ?? [],
  )]
    .sort((left, right) => right.length - left.length)
    .slice(0, 3)
}

async function findPaymentContext(question: string, signal?: AbortSignal): Promise<string> {
  if (!PAYMENT_INTENT.test(question)) return ''

  const terms = paymentSearchTerms(question)
  const results = await Promise.all(
    terms.map((search) =>
      http.get<Page<Payment>>('revenue/payments', {
        query: { page: 1, pageSize: 5, search, sortBy: 'createdAt', sortDir: 'desc' },
        signal,
      }),
    ),
  )
  const payments = [...new Map(results.flatMap((page) => page.items).map((payment) => [payment.id, payment])).values()]

  if (payments.length === 0) {
    return 'No matching payment record was found for the user’s reference. Do not claim to have inspected a payment or invent its details; ask for the customer name, payment ID, or transaction ID.'
  }

  return `Matching payment records from the user's account:\n${JSON.stringify(
    payments.map(({ id, customerName, amount, status, failureCode, createdAt, dataSource }) => ({
      id,
      customerName,
      amount,
      status,
      failureCode,
      createdAt,
      dataSource,
    })),
  )}\nUse only these records for payment-specific claims. If multiple records could match, ask the user which one they mean.`
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
    async *streamChat(request, options) {
      const language = {
        en: 'English',
        de: 'German',
        tr: 'Turkish',
        es: 'Spanish',
      }[getCurrentLocale()]
      const latestQuestion = [...request.messages].reverse().find((message) => message.role === 'user')?.content ?? ''
      const paymentContext = await findPaymentContext(latestQuestion, options?.signal)
      const response = await http.post<CompletionResponse>('gateway/completions', {
        body: {
          model: 'auto',
          taskType: 'default',
          messages: [
            {
              role: 'system',
              content: [
                `You are a helpful PayPal Revenue Intelligence assistant. Reply in ${language}.`,
                paymentContext,
              ].filter(Boolean).join('\n\n'),
            },
            ...request.messages,
          ],
          cache: true,
          optimizePrompt: true,
        },
        signal: options?.signal,
        timeoutMs: LONG_REQUEST_TIMEOUT_MS,
      })

      yield { type: 'meta', model: response.model, provider: response.provider }
      if (response.content) yield { type: 'token', text: response.content }
      yield {
        type: 'usage',
        model: response.model,
        provider: response.provider,
        inputTokens: response.usage.inputTokens,
        outputTokens: response.usage.outputTokens,
        cost: response.cost,
        cacheHit: response.cache.status === 'hit',
      }
      yield { type: 'done' }
    },
  },
}
