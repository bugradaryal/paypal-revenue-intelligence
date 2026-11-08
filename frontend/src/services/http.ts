import { env } from '@/config/env'
import { getCurrentLocale } from '@/i18n/current'
import { ApiError, NetworkError, TimeoutError } from './errors'

/**
 * The single HTTP client. The base URL comes from VITE_API_BASE_URL (via config/env) and is not
 * repeated anywhere else. Endpoint paths are passed by the real adapter once API_CONTRACT.md defines them.
 */

type QueryValue = string | number | boolean | null | undefined
export type QueryParams = Record<string, QueryValue | QueryValue[]>

export interface RequestConfig {
  query?: QueryParams
  body?: unknown
  headers?: Record<string, string>
  signal?: AbortSignal
  /** Default 15 000 ms. */
  timeoutMs?: number
}

type HeaderProvider = () => Record<string, string> | Promise<Record<string, string>>

let authHeaders: HeaderProvider | null = null
let onUnauthorized: (() => void) | null = null

/** Called when a request that carried a token gets 401 (token expired or invalid). */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler
}

/**
 * Hook for the authentication scheme. The scheme is not decided yet (see docs/API_NEEDS.md),
 * so nothing is attached by default.
 */
export function setAuthHeaderProvider(provider: HeaderProvider | null) {
  authHeaders = provider
}

function buildUrl(path: string, query?: QueryParams): string {
  const base = env.apiBaseUrl.replace(/\/+$/, '')
  const url = new URL(`${base}/${path.replace(/^\/+/, '')}`, window.location.origin)

  for (const [key, value] of Object.entries(query ?? {})) {
    const values = Array.isArray(value) ? value : [value]
    for (const v of values) {
      if (v !== undefined && v !== null && v !== '') url.searchParams.append(key, String(v))
    }
  }
  return url.toString()
}

/** Abort on caller cancel OR timeout, and tell the two apart. */
function withTimeout(signal: AbortSignal | undefined, timeoutMs: number) {
  const controller = new AbortController()
  let timedOut = false

  const timer = window.setTimeout(() => {
    timedOut = true
    controller.abort()
  }, timeoutMs)

  const onAbort = () => controller.abort()
  if (signal?.aborted) controller.abort()
  else signal?.addEventListener('abort', onAbort, { once: true })

  return {
    signal: controller.signal,
    didTimeOut: () => timedOut,
    cleanup: () => {
      window.clearTimeout(timer)
      signal?.removeEventListener('abort', onAbort)
    },
  }
}

async function parseBody(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined
  const text = await response.text()
  if (!text) return undefined
  const type = response.headers.get('content-type') ?? ''
  if (type.includes('json')) {
    try {
      return JSON.parse(text)
    } catch {
      return text
    }
  }
  return text
}

/** Tolerant reader: understands RFC 7807 ProblemDetails, falls back to status text. */
function toApiError(response: Response, body: unknown): ApiError {
  const problem = typeof body === 'object' && body !== null ? (body as Record<string, unknown>) : {}
  const str = (v: unknown) => (typeof v === 'string' ? v : undefined)

  return new ApiError({
    status: response.status,
    title: str(problem.title) ?? (response.statusText || `HTTP ${response.status}`),
    detail: str(problem.detail),
    code: str(problem.code) ?? str(problem.type),
    fieldErrors:
      typeof problem.errors === 'object' && problem.errors !== null
        ? (problem.errors as Record<string, string[]>)
        : undefined,
    body,
  })
}

async function request<T>(method: string, path: string, config: RequestConfig = {}): Promise<T> {
  const { query, body, headers, signal, timeoutMs = 15_000 } = config
  const timeout = withTimeout(signal, timeoutMs)

  try {
    const extra = authHeaders ? await authHeaders() : {}
    const response = await fetch(buildUrl(path, query), {
      method,
      signal: timeout.signal,
      headers: {
        Accept: 'application/json',
        'Accept-Language': getCurrentLocale(),
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
        ...extra,
        ...headers,
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })

    const parsed = await parseBody(response)
    if (!response.ok) {
      if (response.status === 401 && 'Authorization' in { ...extra, ...headers }) onUnauthorized?.()
      throw toApiError(response, parsed)
    }
    return parsed as T
  } catch (error) {
    if (error instanceof ApiError) throw error
    if (error instanceof DOMException && error.name === 'AbortError') {
      // Caller cancellation stays an AbortError (React Query ignores it); a timeout becomes its own error.
      if (timeout.didTimeOut()) throw new TimeoutError()
      throw error
    }
    throw new NetworkError(error instanceof Error ? error.message : undefined)
  } finally {
    timeout.cleanup()
  }
}

export const http = {
  get: <T>(path: string, config?: RequestConfig) => request<T>('GET', path, config),
  post: <T>(path: string, config?: RequestConfig) => request<T>('POST', path, config),
  put: <T>(path: string, config?: RequestConfig) => request<T>('PUT', path, config),
  patch: <T>(path: string, config?: RequestConfig) => request<T>('PATCH', path, config),
  delete: <T>(path: string, config?: RequestConfig) => request<T>('DELETE', path, config),
}
