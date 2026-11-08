import { http, setAuthHeaderProvider, setUnauthorizedHandler } from './http'
import type { AuthUser, LoginRequest, LoginResponse } from './types'

/**
 * Session handling for the JWT scheme in docs/API_CONTRACT.md section 4:
 * POST /auth/login returns an access token (60 min, no refresh); a 401 means "log in again".
 * The token lives in sessionStorage, so closing the tab ends the session.
 */

const KEY = 'pri-session'

interface Session {
  accessToken: string
  expiresAtUtc: string
  user: AuthUser
}

export type LogoutReason = 'expired' | 'manual' | null

let session: Session | null = load()
let lastReason: LogoutReason = null
const listeners = new Set<() => void>()

function load(): Session | null {
  try {
    const raw = sessionStorage.getItem(KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as Session
    return Date.parse(parsed.expiresAtUtc) > Date.now() ? parsed : null
  } catch {
    return null
  }
}

function emit() {
  listeners.forEach((fn) => fn())
}

function store(next: Session | null) {
  session = next
  try {
    if (next) sessionStorage.setItem(KEY, JSON.stringify(next))
    else sessionStorage.removeItem(KEY)
  } catch {
    /* storage unavailable: the session then lasts until reload */
  }
  emit()
}

export const getSession = () => session
export const getLogoutReason = () => lastReason
export const subscribeSession = (fn: () => void) => {
  listeners.add(fn)
  return () => listeners.delete(fn)
}

export async function login(request: LoginRequest, signal?: AbortSignal): Promise<AuthUser> {
  const response = await http.post<LoginResponse>('auth/login', { body: request, signal })
  lastReason = null
  store({ accessToken: response.accessToken, expiresAtUtc: response.expiresAtUtc, user: response.user })
  return response.user
}

export function logout(reason: Exclude<LogoutReason, null> = 'manual') {
  lastReason = reason
  store(null)
}

setAuthHeaderProvider((): Record<string, string> => (session ? { Authorization: `Bearer ${session.accessToken}` } : {}))
setUnauthorizedHandler(() => logout('expired'))
