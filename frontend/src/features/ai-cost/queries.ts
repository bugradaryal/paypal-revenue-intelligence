import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '@/services/adapters'
import type { DateRange } from '@/services/types'

export const aiCostKeys = {
  all: ['ai-cost'] as const,
  summary: (range: DateRange) => [...aiCostKeys.all, 'summary', range] as const,
  series: (range: DateRange) => [...aiCostKeys.all, 'series', range] as const,
  breakdown: (range: DateRange) => [...aiCostKeys.all, 'breakdown', range] as const,
}

export function useAiUsageSummary(range: DateRange) {
  return useQuery({
    queryKey: aiCostKeys.summary(range),
    queryFn: ({ signal }) => api.aiCost.getUsageSummary(range, { signal }),
    placeholderData: keepPreviousData,
  })
}

export function useAiUsageSeries(range: DateRange) {
  return useQuery({
    queryKey: aiCostKeys.series(range),
    queryFn: ({ signal }) => api.aiCost.getUsageSeries(range, { signal }),
    placeholderData: keepPreviousData,
  })
}

export function useAiBreakdown(range: DateRange) {
  return useQuery({
    queryKey: aiCostKeys.breakdown(range),
    queryFn: ({ signal }) => api.aiCost.getBreakdown(range, { signal }),
    placeholderData: keepPreviousData,
  })
}
