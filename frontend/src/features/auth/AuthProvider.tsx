import { createContext, useContext, useMemo, useSyncExternalStore, type ReactNode } from 'react'
import { env } from '@/config/env'
import { getSession, login, logout, subscribeSession } from '@/services/auth'
import type { AuthUser } from '@/services/types'

/**
 * Mock mode has one built-in demo user and no login. Real mode needs a JWT session
 * (docs/API_CONTRACT.md section 4).
 */

interface AuthContextValue {
  /** false in mock mode: nothing is protected. */
  required: boolean
  user: AuthUser | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const session = useSyncExternalStore(subscribeSession, getSession, getSession)

  const value = useMemo<AuthContextValue>(
    () => ({
      required: !env.isMock,
      user: session?.user ?? null,
      signIn: async (email, password) => {
        await login({ email, password })
      },
      signOut: () => logout('manual'),
    }),
    [session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
