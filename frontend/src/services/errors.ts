export interface ApiErrorInit {
  status: number
  title: string
  detail?: string
  /** Stable machine-readable code (if the backend provides one). The UI translates by code, never shows raw server text. */
  code?: string
  fieldErrors?: Record<string, string[]>
  body?: unknown
}

/** The backend answered with a non-2xx status. */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly code?: string
  readonly fieldErrors?: Record<string, string[]>
  readonly body?: unknown

  constructor(init: ApiErrorInit) {
    super(init.detail ?? init.title)
    this.name = 'ApiError'
    this.status = init.status
    this.title = init.title
    this.detail = init.detail
    this.code = init.code
    this.fieldErrors = init.fieldErrors
    this.body = init.body
  }
}

/** The request never produced a response (offline, DNS, CORS, backend down). */
export class NetworkError extends Error {
  constructor(message = 'Network request failed') {
    super(message)
    this.name = 'NetworkError'
  }
}

/** The request took longer than the configured timeout. */
export class TimeoutError extends Error {
  constructor(message = 'Request timed out') {
    super(message)
    this.name = 'TimeoutError'
  }
}

/**
 * Thrown by the real adapter for operations whose endpoint is not yet defined in API_CONTRACT.md.
 * The frontend never guesses endpoints.
 */
export class ContractNotReadyError extends Error {
  readonly operation: string

  constructor(operation: string) {
    super(`"${operation}" is not defined in API_CONTRACT.md yet.`)
    this.name = 'ContractNotReadyError'
    this.operation = operation
  }
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}
