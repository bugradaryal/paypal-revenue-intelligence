import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http } from '@/services/http'
import type { AdminUser, CreateAdminUserRequest } from '@/services/types'

export const userKeys = {
  all: ['admin-users'] as const,
}

export const useAdminUsers = () =>
  useQuery({
    queryKey: userKeys.all,
    queryFn: ({ signal }) => http.get<AdminUser[]>('admin/users', { signal }),
  })

export function useCreateAdminUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateAdminUserRequest) =>
      http.post<AdminUser>('admin/users', { body: request }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: userKeys.all }),
  })
}

export function useDeleteAdminUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => http.delete<void>(`admin/users/${encodeURIComponent(id)}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: userKeys.all }),
  })
}
