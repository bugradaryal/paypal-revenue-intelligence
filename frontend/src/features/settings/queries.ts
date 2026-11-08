import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { aiCostKeys } from '@/features/ai-cost/queries'
import { api } from '@/services/adapters'
import type { UpdateBudgetRequest } from '@/services/types'

export const settingsKeys = {
  all: ['settings'] as const,
  budget: () => [...settingsKeys.all, 'budget'] as const,
  models: () => [...settingsKeys.all, 'models'] as const,
  cache: () => [...settingsKeys.all, 'cache'] as const,
}

export const useBudgetSetting = () =>
  useQuery({ queryKey: settingsKeys.budget(), queryFn: ({ signal }) => api.settings.getBudget({ signal }) })

export const useModelCatalog = () =>
  useQuery({ queryKey: settingsKeys.models(), queryFn: ({ signal }) => api.settings.listModels({ signal }) })

export const useCacheStats = () =>
  useQuery({ queryKey: settingsKeys.cache(), queryFn: ({ signal }) => api.settings.getCacheStats({ signal }) })

export function useUpdateBudget() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: UpdateBudgetRequest) => api.settings.setBudget(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: settingsKeys.budget() })
      // The AI Cost page shows this budget.
      void queryClient.invalidateQueries({ queryKey: aiCostKeys.all })
    },
  })
}

export function useClearCache() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.settings.clearCache(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: settingsKeys.cache() }),
  })
}
