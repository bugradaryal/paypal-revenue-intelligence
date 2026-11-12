import { en } from '@/i18n/locales/en'
import { de } from '@/i18n/locales/de'
import { tr } from '@/i18n/locales/tr'
import { es } from '@/i18n/locales/es'
import { getCurrentLocale } from '@/i18n/current'
import { formatMoney, formatPercent } from '@/lib/format'
import { lastDays } from '@/lib/dates'
import type { ChatEvent, ChatRequest, RequestOptions } from '../../types'
import {
  buildAiUsageSummary,
  buildRecoverySummary,
  buildRevenueSummary,
  getMockPayments,
  getSuggestionStore,
  localizeSuggestion,
} from './data'
import { isMockFailing } from './failure'

/**
 * The local assistant recognizes a few topics, responds from the bundled demo dataset, and streams
 * text word by word for exercising the Workspace UI before the real streaming schema is available.
 */

type Lang = 'en' | 'de' | 'tr' | 'es'
type Topic = 'failures' | 'recovery' | 'cost' | 'help'

const INTL: Record<Lang, string> = { en: 'en-US', de: 'de-DE', tr: 'tr-TR', es: 'es-ES' }
const DICT = { en, de, tr, es }

const KEYWORDS: Record<Exclude<Topic, 'help'>, string[]> = {
  failures: ['fail', 'declin', 'başarısız', 'basarisiz', 'neden', 'fehl', 'abgelehnt', 'fallo', 'fallid', 'rechaz'],
  recovery: ['recover', 'kurtar', 'rückgew', 'zurückgew', 'recuper'],
  cost: ['cost', 'spend', 'maliyet', 'harcama', 'kosten', 'ausgaben', 'costo', 'gasto', 'budget', 'bütçe', 'presupuesto'],
}

const TEXT: Record<Lang, Record<Topic | 'action', string>> = {
  en: {
    failures: 'Here is what I see for the last 30 days:\n- {count} failed payments worth {amount}\n- Most common reason: {reason} ({top} payments)',
    action: '\n\nI prepared a suggested action for one open case. Nothing is sent until you approve it.',
    recovery: 'Recovery over the last 30 days:\n- Lost: {lost}\n- Recovered: {recovered}\n- Recovery rate: {rate}\n- {open} suggestions are waiting for your decision.',
    cost: 'AI usage over the last 30 days:\n- Cost: {cost} for {requests} requests\n- Cache hit rate: {rate}\n- Savings measured by the gateway: {savings}',
    help: 'I can look at failed payments, revenue recovery and AI cost. Try one of the questions below.\n\nI never change anything on my own: every action needs your approval.',
  },
  de: {
    failures: 'Das sehe ich für die letzten 30 Tage:\n- {count} fehlgeschlagene Zahlungen im Wert von {amount}\n- Häufigster Grund: {reason} ({top} Zahlungen)',
    action: '\n\nIch habe für einen offenen Fall eine Aktion vorbereitet. Nichts wird gesendet, bis du sie freigibst.',
    recovery: 'Rückgewinnung in den letzten 30 Tagen:\n- Verloren: {lost}\n- Zurückgewonnen: {recovered}\n- Rückgewinnungsquote: {rate}\n- {open} Vorschläge warten auf deine Entscheidung.',
    cost: 'KI-Nutzung in den letzten 30 Tagen:\n- Kosten: {cost} für {requests} Anfragen\n- Cache-Trefferquote: {rate}\n- Vom Gateway gemessene Einsparungen: {savings}',
    help: 'Ich kann fehlgeschlagene Zahlungen, Umsatzrückgewinnung und KI-Kosten ansehen. Probiere eine der Fragen unten.\n\nIch ändere nie etwas von selbst: Jede Aktion braucht deine Freigabe.',
  },
  tr: {
    failures: 'Son 30 gün için gördüklerim:\n- {amount} değerinde {count} başarısız ödeme\n- En sık neden: {reason} ({top} ödeme)',
    action: '\n\nAçık bir vaka için önerilen bir işlem hazırladım. Sen onaylamadan hiçbir şey gönderilmez.',
    recovery: 'Son 30 günde kurtarma:\n- Kaybedilen: {lost}\n- Kurtarılan: {recovered}\n- Kurtarma oranı: {rate}\n- {open} öneri kararını bekliyor.',
    cost: 'Son 30 günde yapay zekâ kullanımı:\n- Maliyet: {requests} istek için {cost}\n- Önbellek isabet oranı: {rate}\n- Ağ geçidinin ölçtüğü tasarruf: {savings}',
    help: 'Başarısız ödemelere, gelir kurtarmaya ve yapay zekâ maliyetine bakabilirim. Aşağıdaki sorulardan birini dene.\n\nKendi başıma hiçbir şeyi değiştirmem: her işlem senin onayını ister.',
  },
  es: {
    failures: 'Esto es lo que veo en los últimos 30 días:\n- {count} pagos fallidos por {amount}\n- Motivo más frecuente: {reason} ({top} pagos)',
    action: '\n\nPreparé una acción sugerida para un caso abierto. No se envía nada hasta que la apruebes.',
    recovery: 'Recuperación en los últimos 30 días:\n- Perdido: {lost}\n- Recuperado: {recovered}\n- Tasa de recuperación: {rate}\n- {open} sugerencias esperan tu decisión.',
    cost: 'Uso de IA en los últimos 30 días:\n- Costo: {cost} por {requests} solicitudes\n- Tasa de aciertos de caché: {rate}\n- Ahorro medido por la pasarela: {savings}',
    help: 'Puedo revisar pagos fallidos, recuperación de ingresos y costo de IA. Prueba una de las preguntas de abajo.\n\nNunca cambio nada por mi cuenta: cada acción necesita tu aprobación.',
  },
}

const fill = (template: string, values: Record<string, string | number>) =>
  template.replace(/\{(\w+)\}/g, (_, k: string) => String(values[k] ?? ''))

function detectTopic(text: string): Topic {
  const lower = text.toLowerCase()
  for (const topic of ['failures', 'recovery', 'cost'] as const) {
    if (KEYWORDS[topic].some((word) => lower.includes(word))) return topic
  }
  return 'help'
}

function sleep(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) return reject(new DOMException('Aborted', 'AbortError'))
    const timer = setTimeout(resolve, ms)
    signal?.addEventListener(
      'abort',
      () => {
        clearTimeout(timer)
        reject(new DOMException('Aborted', 'AbortError'))
      },
      { once: true },
    )
  })
}

function buildAnswer(topic: Topic, lang: Lang) {
  const locale = INTL[lang]
  const money = (m: { amount: string; currency: string }) => formatMoney(m, locale, { maximumFractionDigits: 2 })
  const range = lastDays(30)
  let text = TEXT[lang][topic]

  if (topic === 'failures') {
    const from = Date.parse(range.from)
    const failed = getMockPayments().filter((p) => p.status === 'failed' && Date.parse(p.createdAt) >= from)
    const counts = new Map<string, number>()
    failed.forEach((p) => p.failureCode && counts.set(p.failureCode, (counts.get(p.failureCode) ?? 0) + 1))
    const [code, top] = [...counts.entries()].sort((a, b) => b[1] - a[1])[0] ?? ['other', 0]
    const reasons = DICT[lang].failureCode as Record<string, string>
    const summary = buildRevenueSummary(range)
    text = fill(text, { count: failed.length, amount: money(summary.failedAmount), reason: reasons[code] ?? reasons.other ?? code, top })
  } else if (topic === 'recovery') {
    const r = buildRecoverySummary(range)
    text = fill(text, { lost: money(r.lostAmount), recovered: money(r.recoveredAmount), rate: formatPercent(r.recoveryRate, locale), open: r.openSuggestionCount })
  } else if (topic === 'cost') {
    const c = buildAiUsageSummary(range)
    text = fill(text, {
      cost: money(c.totalCost),
      requests: new Intl.NumberFormat(locale).format(c.totalRequests),
      rate: formatPercent(c.cacheHitRate, locale),
      savings: money(c.savings),
    })
  }

  const suggestion =
    topic === 'failures'
      ? [...getSuggestionStore().values()].find((s) => s.status === 'proposed' && s.executable)
      : undefined
  if (suggestion) text += TEXT[lang].action

  return { text, suggestion: suggestion ? localizeSuggestion(suggestion) : undefined }
}

export function buildMockWorkspaceContext(): string {
  const dateRange = lastDays(30)
  const revenue = buildRevenueSummary(dateRange)
  const recovery = buildRecoverySummary(dateRange)
  const aiUsage = buildAiUsageSummary(dateRange)
  const from = Date.parse(dateRange.from)
  const failedPayments = getMockPayments()
    .filter((payment) => payment.status === 'failed' && Date.parse(payment.createdAt) >= from)
    .sort((a, b) => Number(b.amount.amount) - Number(a.amount.amount))
    .slice(0, 8)
    .map(({ id, customerName, amount, failureCode, createdAt }) => ({
      id,
      customerName,
      amount,
      failureCode,
      createdAt,
    }))
  const openSuggestions = [...getSuggestionStore().values()]
    .filter((suggestion) => suggestion.status === 'proposed')
    .slice(0, 8)
    .map(({ paymentId, customerName, type, expectedRecovery, rationale }) => ({
      paymentId,
      customerName,
      type,
      expectedRecovery,
      rationale,
    }))

  return [
    'This is fictional local demo data, not live PayPal or verified business data. Answer using this snapshot when relevant and state that it is demo data. Failed payment amounts are exposure, not confirmed lost revenue; expected recovery is an estimate. AI usage figures here are demo-only, not actual account totals; the per-answer receipt shows the actual completion cost. Do not claim to have changed or contacted anything. Recommendations are advisory only.',
    `Date range: ${dateRange.from} to ${dateRange.to} (end exclusive).`,
    JSON.stringify(
      {
        revenue: {
          total: revenue.totalRevenue,
          failed: revenue.failedAmount,
          pending: revenue.pendingAmount,
          recovered: revenue.recoveredRevenue,
          paymentCount: revenue.paymentCount,
          failedCount: revenue.failedCount,
          pendingCount: revenue.pendingCount,
        },
        recovery: {
          failedPaymentExposure: recovery.lostAmount,
          recoveredFromFailures: recovery.recoveredAmount,
          recoveryRate: recovery.recoveryRate,
          openSuggestionCount: recovery.openSuggestionCount,
        },
        aiUsageDemoOnly: {
          cost: aiUsage.totalCost,
          requests: aiUsage.totalRequests,
          inputTokens: aiUsage.inputTokens,
          outputTokens: aiUsage.outputTokens,
          cacheHitRate: aiUsage.cacheHitRate,
          savings: aiUsage.savings,
        },
        failedPayments,
        openSuggestions,
      },
      null,
      2,
    ),
  ].join('\n\n')
}

const seenQuestions = new Set<string>()

export async function* mockStreamChat(request: ChatRequest, options?: RequestOptions): AsyncGenerator<ChatEvent> {
  const signal = options?.signal
  const lang = (['en', 'de', 'tr', 'es'].includes(getCurrentLocale()) ? getCurrentLocale() : 'en') as Lang
  const question = [...request.messages].reverse().find((m) => m.role === 'user')?.content ?? ''
  const key = `${lang}:${question.trim().toLowerCase()}`
  const cacheHit = seenQuestions.has(key)
  seenQuestions.add(key)

  await sleep(450, signal)
  if (isMockFailing()) {
    yield { type: 'error', code: 'mock_failure' }
    return
  }

  const model = 'gpt-4o-mini'
  const provider = 'openai'
  yield { type: 'meta', model, provider }

  const { text, suggestion } = buildAnswer(detectTopic(question), lang)
  const parts = text.split(/(\s+)/).filter(Boolean)
  for (const part of parts) {
    await sleep(cacheHit ? 4 : 22, signal)
    yield { type: 'token', text: part }
  }

  if (suggestion) yield { type: 'suggestion', suggestion }

  // Mock figures only: a repeated question is answered from the cache and costs nothing.
  const inputTokens = cacheHit ? 0 : Math.max(20, Math.round((question.length + 160) / 3.6))
  const outputTokens = cacheHit ? 0 : Math.max(20, Math.round(text.length / 3.6))
  const cost = cacheHit ? 0 : (inputTokens * 0.15 + outputTokens * 0.6) / 1_000_000
  yield { type: 'usage', model, provider, inputTokens, outputTokens, cost: { amount: cost.toFixed(6), currency: 'USD' }, cacheHit }
  yield { type: 'done' }
}
