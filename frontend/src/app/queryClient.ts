import { QueryClient } from '@tanstack/react-query'
import { ApiError, ContractNotReadyError } from '@/services/errors'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        // Simulated failures are deliberate: show them immediately.
        if (error instanceof ApiError && error.code === 'mock_failure') return false
        if (error instanceof ContractNotReadyError) return false
        // 4xx means the request itself is wrong; retrying cannot help.
        if (error instanceof ApiError && error.status < 500) return false
        return failureCount < 2
      },
    },
  },
})
