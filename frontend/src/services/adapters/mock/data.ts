/**
 * The local demo dataset is fictional and generated deterministically. It stands in for
 * operations that are not yet available through the API; it contains no real payments,
 * customers, or measured AI savings.
 */
import { getCurrentLocale } from '@/i18n/current'
import { DAY_MS, startOfUtcDay } from '@/lib/dates'
import type {
  AiBudgetSetting,
  AiCostBreakdown,
  AiCostBreakdownItem,
  AiCostPoint,
  AiModelInfo,
  AiUsageSummary,
  DateRange,
  Money,
  Payment,
  PaymentDetail,
  RecoverySuggestion,
  RecoverySuggestionType,
  RecoverySummary,
  RevenuePoint,
  RevenueSummary,
} from '../../types'
import { mulberry32, pick } from './random'

const CURRENCY = 'USD'

const centsToMoney = (cents: number): Money => ({ amount: (cents / 100).toFixed(2), currency: CURRENCY })
const usdToMoney = (usd: number): Money => ({ amount: usd.toFixed(2), currency: CURRENCY })

const inRange = (ms: number, range: DateRange) => ms >= Date.parse(range.from) && ms < Date.parse(range.to)

/** Whole UTC days covered by a range. */
function eachDay(range: DateRange): number[] {
  const days: number[] = []
  for (let d = startOfUtcDay(Date.parse(range.from)); d < Date.parse(range.to); d += DAY_MS) days.push(d)
  return days
}

// ---------------------------------------------------------------------------
// Payments
// ---------------------------------------------------------------------------

interface MockPayment extends Payment {
  cents: number
  recoveredFromFailure: boolean
}

const CUSTOMERS = [
  'Acme GmbH',
  'Nova Studio',
  'Lumen Labs',
  'Kestrel Foods',
  'Orbit Logistics',
  'Verde Interiors',
  'Pixel Forge',
  'Harbor & Co',
  'Atlas Fitness',
  'Mira Boutique',
  'Delta Dental',
  'Sunrise Bakery',
] as const

const FAILURE_CODES = ['card_declined', 'insufficient_funds', 'expired_card', 'account_restricted'] as const

let paymentCache: MockPayment[] | null = null

export function getMockPayments(): MockPayment[] {
  if (paymentCache) return paymentCache

  const rnd = mulberry32(20261106)
  const now = Date.now()
  const generated: Omit<MockPayment, 'id'>[] = []

  for (let i = 0; i < 260; i++) {
    const createdMs = Math.min(now - 60_000, startOfUtcDay(now) + DAY_MS - Math.floor(rnd() * 120 * DAY_MS))
    const ageDays = (now - createdMs) / DAY_MS
    const customerIndex = Math.floor(rnd() * CUSTOMERS.length)
    const cents = Math.round((20 + rnd() ** 2 * 2400) * 100)

    const r = rnd()
    let status: Payment['status'] = 'completed'
    if (r >= 0.78 && r < 0.9) status = 'failed'
    else if (r >= 0.9 && r < 0.97) status = ageDays < 3 ? 'pending' : 'completed'
    else if (r >= 0.97) status = 'refunded'

    const recovered = status === 'completed' && rnd() < 0.07
    const updatedMs = Math.min(now, createdMs + (recovered ? (1 + rnd() * 2) * DAY_MS : rnd() * 6 * 3_600_000))

    generated.push({
      customerId: `cus_${String(customerIndex + 1).padStart(3, '0')}`,
      customerName: CUSTOMERS[customerIndex] as string,
      amount: centsToMoney(cents),
      status,
      failureCode: status === 'failed' ? pick(FAILURE_CODES, rnd) : null,
      paypalTransactionId: null,
      dataSource: 'demo',
      createdAt: new Date(createdMs).toISOString(),
      updatedAt: new Date(updatedMs).toISOString(),
      cents,
      recoveredFromFailure: recovered,
    })
  }

  generated.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
  paymentCache = generated.map((p, index) => ({ ...p, id: `PAY-${2000 + generated.length - index}` }))
  return paymentCache
}

/** Strip mock-only fields. */
export function toPayment(p: MockPayment): Payment {
  return {
    id: p.id,
    customerId: p.customerId,
    customerName: p.customerName,
    amount: p.amount,
    status: p.status,
    failureCode: p.failureCode,
    paypalTransactionId: p.paypalTransactionId,
    dataSource: p.dataSource,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
  }
}

export function toPaymentDetail(p: MockPayment): PaymentDetail {
  const history: PaymentDetail['statusHistory'] = [{ status: 'pending', occurredAt: p.createdAt }]
  if (p.status !== 'pending') history.push({ status: p.status, occurredAt: p.updatedAt })
  return { ...toPayment(p), statusHistory: history }
}

export function buildRevenueSummary(range: DateRange): RevenueSummary {
  const payments = getMockPayments().filter((p) => inRange(Date.parse(p.createdAt), range))

  let revenue = 0
  let failed = 0
  let pending = 0
  let recovered = 0
  let failedCount = 0
  let pendingCount = 0

  const daily = new Map<number, { revenue: number; recovered: number }>()
  for (const d of eachDay(range)) daily.set(d, { revenue: 0, recovered: 0 })

  for (const p of payments) {
    const bucket = daily.get(startOfUtcDay(Date.parse(p.createdAt)))
    if (p.status === 'completed') {
      revenue += p.cents
      if (bucket) bucket.revenue += p.cents
      if (p.recoveredFromFailure) {
        recovered += p.cents
        if (bucket) bucket.recovered += p.cents
      }
    } else if (p.status === 'failed') {
      failed += p.cents
      failedCount++
    } else if (p.status === 'pending') {
      pending += p.cents
      pendingCount++
    }
  }

  const series: RevenuePoint[] = [...daily.entries()].map(([date, v]) => ({
    date: new Date(date).toISOString(),
    revenue: centsToMoney(v.revenue),
    recovered: centsToMoney(v.recovered),
  }))

  return {
    range,
    totalRevenue: centsToMoney(revenue),
    failedAmount: centsToMoney(failed),
    pendingAmount: centsToMoney(pending),
    recoveredRevenue: centsToMoney(recovered),
    paymentCount: payments.length,
    failedCount,
    pendingCount,
    excludedPaymentCount: 0,
    series,
  }
}

// ---------------------------------------------------------------------------
// Recovery
// ---------------------------------------------------------------------------

type RationaleLocale = 'en' | 'de' | 'tr' | 'es'

// The real backend writes this text (see docs/API_NEEDS.md, "AI text language") and honours Accept-Language.
// The mock mimics that so the UI can be checked in every language.
const SUGGESTION_BY_FAILURE: Record<string, { type: RecoverySuggestionType; rationale: Record<RationaleLocale, string> }> = {
  card_declined: {
    type: 'send_reminder',
    rationale: {
      en: 'The card was declined by the issuer. Suggest sending a reminder with a fresh payment link.',
      de: 'Die Karte wurde vom Aussteller abgelehnt. Vorschlag: eine Erinnerung mit neuem Zahlungslink senden.',
      tr: 'Kart, kartı çıkaran banka tarafından reddedildi. Yeni bir ödeme bağlantısıyla hatırlatma göndermeyi öneriyoruz.',
      es: 'La tarjeta fue rechazada por el emisor. Se sugiere enviar un recordatorio con un nuevo enlace de pago.',
    },
  },
  insufficient_funds: {
    type: 'retry_payment',
    rationale: {
      en: 'Insufficient funds at the time of payment. Suggest retrying in three days.',
      de: 'Zum Zahlungszeitpunkt war das Guthaben nicht ausreichend. Vorschlag: in drei Tagen erneut versuchen.',
      tr: 'Ödeme anında bakiye yetersizdi. Üç gün sonra yeniden denemeyi öneriyoruz.',
      es: 'Fondos insuficientes en el momento del pago. Se sugiere reintentar en tres días.',
    },
  },
  expired_card: {
    type: 'send_reminder',
    rationale: {
      en: 'The saved card has expired. Suggest asking the customer to update the payment method.',
      de: 'Die gespeicherte Karte ist abgelaufen. Vorschlag: den Kunden bitten, die Zahlungsart zu aktualisieren.',
      tr: 'Kayıtlı kartın süresi dolmuş. Müşteriden ödeme yöntemini güncellemesini istemeyi öneriyoruz.',
      es: 'La tarjeta guardada ha caducado. Se sugiere pedir al cliente que actualice el método de pago.',
    },
  },
  account_restricted: {
    type: 'offer_alternative_method',
    rationale: {
      en: 'The PayPal account is restricted. Suggest offering an alternative payment method.',
      de: 'Das PayPal-Konto ist eingeschränkt. Vorschlag: eine andere Zahlungsart anbieten.',
      tr: 'PayPal hesabı kısıtlı. Başka bir ödeme yöntemi sunmayı öneriyoruz.',
      es: 'La cuenta de PayPal está restringida. Se sugiere ofrecer un método de pago alternativo.',
    },
  },
}

/** Which failure each suggestion came from, so its text can be produced in the active language. */
const suggestionFailure = new Map<string, string>()

export function localizeSuggestion(s: RecoverySuggestion): RecoverySuggestion {
  const code = suggestionFailure.get(s.id)
  const plan = code ? SUGGESTION_BY_FAILURE[code] : undefined
  if (!plan) return s
  const locale = getCurrentLocale() as RationaleLocale
  const language: RationaleLocale = locale in plan.rationale ? locale : 'en'
  return { ...s, rationale: plan.rationale[language], rationaleLanguage: language }
}

let suggestionStore: Map<string, RecoverySuggestion> | null = null

export function getSuggestionStore(): Map<string, RecoverySuggestion> {
  if (suggestionStore) return suggestionStore

  const failed = getMockPayments()
    .filter((p) => p.status === 'failed' && p.failureCode)
    .slice(0, 14)

  suggestionStore = new Map(
    failed.map((p, index): [string, RecoverySuggestion] => {
      const plan = SUGGESTION_BY_FAILURE[p.failureCode as string] as { type: RecoverySuggestionType; rationale: Record<RationaleLocale, string> }
      const executed = index >= failed.length - 2
      const suggestion: RecoverySuggestion = {
        id: `SUG-${String(index + 1).padStart(3, '0')}`,
        paymentId: p.id,
        customerName: p.customerName,
        type: plan.type,
        rationale: plan.rationale.en,
        rationaleLanguage: 'en',
        expectedRecovery: p.amount,
        status: executed ? 'executed' : 'proposed',
        executable: plan.type === 'send_reminder',
        dataSource: 'demo',
        createdAt: p.updatedAt,
        updatedAt: p.updatedAt,
      }
      suggestionFailure.set(suggestion.id, p.failureCode as string)
      return [suggestion.id, suggestion]
    }),
  )
  return suggestionStore
}

/** Mock of POST /revenue/suggestions/generate: proposes actions for failed payments that have none yet. */
export function generateMockSuggestions(limit = 3): number {
  const store = getSuggestionStore()
  const covered = new Set([...store.values()].map((s) => s.paymentId))
  const fresh = getMockPayments()
    .filter((p) => p.status === 'failed' && p.failureCode && !covered.has(p.id))
    .slice(0, limit)

  for (const p of fresh) {
    const plan = SUGGESTION_BY_FAILURE[p.failureCode as string]
    if (!plan) continue
    const now = new Date().toISOString()
    const id = `SUG-${String(store.size + 1).padStart(3, '0')}`
    store.set(id, {
      id,
      paymentId: p.id,
      customerName: p.customerName,
      type: plan.type,
      rationale: plan.rationale.en,
      rationaleLanguage: 'en',
      expectedRecovery: p.amount,
      status: 'proposed',
      executable: plan.type === 'send_reminder',
      dataSource: 'demo',
      createdAt: now,
      updatedAt: now,
    })
    suggestionFailure.set(id, p.failureCode as string)
  }
  return fresh.length
}

export function buildRecoverySummary(range: DateRange): RecoverySummary {
  const summary = buildRevenueSummary(range)
  const lost = Number(summary.failedAmount.amount)
  const recovered = Number(summary.recoveredRevenue.amount)

  return {
    range,
    lostAmount: summary.failedAmount,
    recoveredAmount: summary.recoveredRevenue,
    // Contract definition: recoveredAmount / lostAmount, 0 when nothing was lost.
    recoveryRate: lost > 0 ? Math.min(1, recovered / lost) : 0,
    openSuggestionCount: [...getSuggestionStore().values()].filter((s) => s.status === 'proposed').length,
  }
}

// ---------------------------------------------------------------------------
// AI cost (all numbers and prices are fictional)
// ---------------------------------------------------------------------------

interface ModelProfile {
  key: string
  provider: 'openai' | 'google'
  share: number
  /** Fictional price per 1M tokens. */
  inPerM: number
  outPerM: number
}

const MODELS: readonly ModelProfile[] = [
  { key: 'gpt-4o-mini', provider: 'openai', share: 0.42, inPerM: 0.15, outPerM: 0.6 },
  { key: 'gpt-4o', provider: 'openai', share: 0.12, inPerM: 2.5, outPerM: 10 },
  { key: 'gemini-flash', provider: 'google', share: 0.15, inPerM: 0.1, outPerM: 0.4 },
]

const PROVIDER_LABEL: Record<ModelProfile['provider'], string> = {
  openai: 'OpenAI',
  google: 'Google',
}

interface ModelDayStat {
  model: ModelProfile
  requests: number
  cacheHits: number
  /** Billed tokens (cached requests consume none). */
  inputTokens: number
  outputTokens: number
  cost: number
  /** Cost avoided by cache hits. */
  cacheAvoided: number
}

const dayCache = new Map<number, ModelDayStat[]>()

function modelStatsForDay(day: number): ModelDayStat[] {
  const cached = dayCache.get(day)
  if (cached) return cached

  const rnd = mulberry32(Math.floor(day / DAY_MS))
  const weekday = new Date(day).getUTCDay()
  const weekendFactor = weekday === 0 || weekday === 6 ? 0.6 : 1
  const totalRequests = (9000 + rnd() * 9000) * weekendFactor

  const stats = MODELS.map((model): ModelDayStat => {
    const requests = Math.round(totalRequests * model.share * (0.85 + rnd() * 0.3))
    const hitRate = 0.24 + rnd() * 0.14
    const cacheHits = Math.round(requests * hitRate)
    const billed = requests - cacheHits
    const inputTokens = Math.round(billed * (700 + rnd() * 500))
    const outputTokens = Math.round(billed * (180 + rnd() * 160))
    const cost = (inputTokens * model.inPerM + outputTokens * model.outPerM) / 1_000_000
    const perRequest = billed > 0 ? cost / billed : 0
    return { model, requests, cacheHits, inputTokens, outputTokens, cost, cacheAvoided: perRequest * cacheHits }
  })

  dayCache.set(day, stats)
  return stats
}

const ROUTING_SAVING_RATIO = 0.1 // fictional

export function buildModelCatalog(): AiModelInfo[] {
  return MODELS.map((m) => ({
    key: m.key,
    modelId: m.key,
    label: m.key,
    provider: PROVIDER_LABEL[m.provider],
    inputPricePerMillionTokens: { amount: m.inPerM.toFixed(2), currency: CURRENCY },
    outputPricePerMillionTokens: { amount: m.outPerM.toFixed(2), currency: CURRENCY },
    baseline: m.key === 'gpt-4o',
  }))
}

/** Mock of the single monthly budget record. Settings edit it; the AI Cost page reads it. */
let mockBudget: AiBudgetSetting | null = { limit: usdToMoney(1500), period: 'monthly' }
export const getMockBudget = () => mockBudget
export const setMockBudget = (value: AiBudgetSetting | null) => {
  mockBudget = value
}

export function buildAiUsageSummary(range: DateRange): AiUsageSummary {
  let cost = 0
  let requests = 0
  let hits = 0
  let input = 0
  let output = 0
  let avoided = 0

  for (const day of eachDay(range)) {
    for (const s of modelStatsForDay(day)) {
      cost += s.cost
      requests += s.requests
      hits += s.cacheHits
      input += s.inputTokens
      output += s.outputTokens
      avoided += s.cacheAvoided + s.cost * ROUTING_SAVING_RATIO
    }
  }

  const now = Date.now()
  const monthStart = Date.UTC(new Date(now).getUTCFullYear(), new Date(now).getUTCMonth(), 1)
  let spent = 0
  for (let d = monthStart; d <= startOfUtcDay(now); d += DAY_MS) {
    for (const s of modelStatsForDay(d)) spent += s.cost
  }

  return {
    range,
    totalCost: usdToMoney(cost),
    totalRequests: requests,
    inputTokens: input,
    outputTokens: output,
    cacheHitRate: requests > 0 ? hits / requests : 0,
    savings: usdToMoney(avoided),
    budget: mockBudget ? { limit: mockBudget.limit, spent: usdToMoney(spent), period: 'monthly' } : null,
  }
}

export function buildAiSeries(range: DateRange): AiCostPoint[] {
  return eachDay(range).map((day) => {
    const stats = modelStatsForDay(day)
    return {
      date: new Date(day).toISOString(),
      cost: usdToMoney(stats.reduce((sum, s) => sum + s.cost, 0)),
      requests: stats.reduce((sum, s) => sum + s.requests, 0),
    }
  })
}

export function buildAiBreakdown(range: DateRange): AiCostBreakdown {
  const byModel = new Map<string, { label: string; cost: number; requests: number; input: number; output: number }>()
  const byProvider = new Map<string, { label: string; cost: number; requests: number; input: number; output: number }>()

  const add = (
    map: typeof byModel,
    key: string,
    label: string,
    s: ModelDayStat,
  ) => {
    const row = map.get(key) ?? { label, cost: 0, requests: 0, input: 0, output: 0 }
    row.cost += s.cost
    row.requests += s.requests
    row.input += s.inputTokens
    row.output += s.outputTokens
    map.set(key, row)
  }

  for (const day of eachDay(range)) {
    for (const s of modelStatsForDay(day)) {
      add(byModel, s.model.key, s.model.key, s)
      add(byProvider, s.model.provider, PROVIDER_LABEL[s.model.provider], s)
    }
  }

  const toItems = (map: typeof byModel): AiCostBreakdownItem[] =>
    [...map.entries()]
      .map(([key, v]) => ({
        key,
        label: v.label,
        cost: usdToMoney(v.cost),
        requests: v.requests,
        inputTokens: v.input,
        outputTokens: v.output,
      }))
      .sort((a, b) => Number(b.cost.amount) - Number(a.cost.amount))

  return { byModel: toItems(byModel), byProvider: toItems(byProvider) }
}
