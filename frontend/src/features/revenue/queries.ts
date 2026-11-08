import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/adapters'
import type { DateRange, DecideSuggestionRequest, PaymentQuery, SuggestionQuery } from '@/services/types'

export const revenueKeys = {
  all: ['revenue'] as const,
  summary: (range: DateRange) => [...revenueKeys.all, 'summary', range] as const,
  payments: (query: PaymentQuery) => [...revenueKeys.all, 'payments', query] as const,
  payment: (id: string) => [...revenueKeys.all, 'payment', id] as const,
  recovery: () => [...revenueKeys.all, 'recovery'] as const,
  recoverySummary: (range: DateRange) => [...revenueKeys.recovery(), 'summary', range] as const,
  suggestions: (query: SuggestionQuery) => [...revenueKeys.recovery(), 'suggestions', query] as const,
}

export function useRevenueSummary(range: DateRange) {
  return useQuery({
    queryKey: revenueKeys.summary(range),
    queryFn: ({ signal }) => api.revenue.getSummary(range, { signal }),
    // Switching the time range keeps the previous figures on screen until the new ones arrive.
    placeholderData: keepPreviousData,
  })
}

export function usePayments(query: PaymentQuery) {
  return useQuery({
    queryKey: revenueKeys.payments(query),
    queryFn: ({ signal }) => api.revenue.listPayments(query, { signal }),
    // Keep the old page visible while the next one loads (no table flicker when paging or filtering).
    placeholderData: keepPreviousData,
  })
}

export function usePayment(id: string | undefined) {
  return useQuery({
    queryKey: revenueKeys.payment(id ?? ''),
    queryFn: ({ signal }) => api.revenue.getPayment(id as string, { signal }),
    enabled: Boolean(id),
  })
}

export function useRecoverySummary(range: DateRange) {
  return useQuery({
    queryKey: revenueKeys.recoverySummary(range),
    queryFn: ({ signal }) => api.revenue.getRecoverySummary(range, { signal }),
    placeholderData: keepPreviousData,
  })
}

/** Contract: after approving an executable suggestion, re-query every 2-3 s for at most 30 s. */
const EXECUTION_POLL_MS = 2500
const EXECUTION_POLL_WINDOW_MS = 30_000

export function useSuggestions(query: SuggestionQuery, options: { enabled?: boolean } = {}) {
  return useQuery({
    enabled: options.enabled ?? true,
    queryKey: revenueKeys.suggestions(query),
    queryFn: ({ signal }) => api.revenue.listSuggestions(query, { signal }),
    placeholderData: keepPreviousData,
    refetchInterval: (state) => {
      const waiting = state.state.data?.items.some(
        (s) => s.status === 'approved' && s.executable && Date.now() - Date.parse(s.updatedAt) < EXECUTION_POLL_WINDOW_MS,
      )
      return waiting ? EXECUTION_POLL_MS : false
    },
  })
}

export function useGenerateSuggestions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => api.revenue.generateSuggestions(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: revenueKeys.recovery() }),
  })
}

/** The approval step for AI-proposed external actions. Nothing is executed without it. */
export function useDecideSuggestion() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, ...request }: { id: string } & DecideSuggestionRequest) => api.revenue.decideSuggestion(id, request),
    // Also after a failure: a 409 means it was already decided elsewhere, so the list must refresh.
    onSettled: () => queryClient.invalidateQueries({ queryKey: revenueKeys.recovery() }),
  })
}
