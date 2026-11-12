import type { IsoDateTime } from './common'

/** Shapes from docs/API_CONTRACT.md v0.3, section 4. */

export type UserRole = 'admin' | 'member'

export interface AuthUser {
  id: string
  email: string
  displayName: string
  role: UserRole
  isActive: boolean
  isPendingApproval: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegistrationRequest {
  email: string
  displayName: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  tokenType: 'Bearer'
  expiresAtUtc: IsoDateTime
  user: AuthUser
}

export interface SystemPing {
  name: string
  version: string
  environment: string
  serverTimeUtc: IsoDateTime
  /** Planned (contract Day 2). */
  paypalEnvironment?: 'sandbox' | 'live'
}
