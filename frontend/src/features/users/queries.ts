import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http } from '@/services/http'
import type { AdminUser, CreateAdminUserRequest } from '@/services/types'

export const userKeys = {
  all: ['admin-users'] as const,
}

export const useAdminUsers = (enabled = true) =>
  useQuery({
    queryKey: userKeys.all,
    queryFn: ({ signal }) => http.get<AdminUser[]>('admin/users', { signal }),
    enabled,
    refetchInterval: enabled ? 60_000 : false,
  })

export function useCreateAdminUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateAdminUserRequest) =>
      http.post<AdminUser>('admin/users', { body: request }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: userKeys.all }),
  })
}

export function useActivateAdminUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      http.post<AdminUser>(`admin/users/${encodeURIComponent(id)}/activate`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: userKeys.all }),
  })
}

export function useDeactivateAdminUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      http.post<AdminUser>(`admin/users/${encodeURIComponent(id)}/deactivate`),
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
