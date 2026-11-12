import type { AuthUser, UserRole } from './auth'

export type AdminUser = AuthUser

export interface CreateAdminUserRequest {
  email: string
  displayName: string
  password: string
  role: UserRole
}
