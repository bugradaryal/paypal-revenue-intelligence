import type {
  AiCostBreakdown,
  AiBudgetSetting,
  AiCostPoint,
  AiModelInfo,
  AiUsageSummary,
  CacheStats,
  ChatEvent,
  ChatRequest,
  ClearCacheResult,
  DateRange,
  DecideSuggestionRequest,
  GenerateSuggestionsResult,
  Page,
  PaymentDetail,
  Payment,
  PaymentQuery,
  RecoverySuggestion,
  RecoverySummary,
  RequestOptions,
  RevenueSummary,
  SuggestionQuery,
  UpdateBudgetRequest,
} from '../types'

/**
 * The contract between the UI and "where data comes from".
 * Pages and hooks only know this interface. `mock/` and `real/` both implement it,
 * so switching is a one-line env change (VITE_DATA_SOURCE).
 */
export interface RevenueApi {
  getSummary(range: DateRange, options?: RequestOptions): Promise<RevenueSummary>
  listPayments(query: PaymentQuery, options?: RequestOptions): Promise<Page<Payment>>
  getPayment(id: string, options?: RequestOptions): Promise<PaymentDetail>
  getRecoverySummary(range: DateRange, options?: RequestOptions): Promise<RecoverySummary>
  listSuggestions(query: SuggestionQuery, options?: RequestOptions): Promise<Page<RecoverySuggestion>>
  decideSuggestion(id: string, request: DecideSuggestionRequest, options?: RequestOptions): Promise<RecoverySuggestion>
  /** AI generation; can take up to 60 s. Never runs automatically. */
  generateSuggestions(options?: RequestOptions): Promise<GenerateSuggestionsResult>
}

export interface AiCostApi {
  getUsageSummary(range: DateRange, options?: RequestOptions): Promise<AiUsageSummary>
  getUsageSeries(range: DateRange, options?: RequestOptions): Promise<AiCostPoint[]>
  getBreakdown(range: DateRange, options?: RequestOptions): Promise<AiCostBreakdown>
}

export interface SettingsApi {
  /** null when no budget has been set. */
  getBudget(options?: RequestOptions): Promise<AiBudgetSetting | null>
  setBudget(request: UpdateBudgetRequest, options?: RequestOptions): Promise<AiBudgetSetting>
  listModels(options?: RequestOptions): Promise<AiModelInfo[]>
  getCacheStats(options?: RequestOptions): Promise<CacheStats>
  clearCache(options?: RequestOptions): Promise<ClearCacheResult>
}

export interface WorkspaceApi {
  /** Streams the assistant's answer. The conversation lives in the client; nothing is stored server-side. */
  streamChat(request: ChatRequest, options?: RequestOptions): AsyncIterable<ChatEvent>
}

export interface Api {
  revenue: RevenueApi
  aiCost: AiCostApi
  settings: SettingsApi
  workspace: WorkspaceApi
}
