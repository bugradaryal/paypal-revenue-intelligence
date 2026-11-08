import type { Money } from './common'
import type { RecoverySuggestion } from './revenue'

/**
 * PROPOSED. The contract fixes only the direction (docs/API_CONTRACT.md v0.3, "AI Workspace"): no server-side
 * conversation, the client sends the history on every request, the answer streams over SSE with the event types
 * meta, token, suggestion, usage, done and error. The exact schema comes in contract 0.4. Until then this
 * is served by the mock adapter and must not be sent to the backend.
 */

export interface ChatMessageInput {
  role: 'user' | 'assistant'
  content: string
}

export interface ChatRequest {
  messages: ChatMessageInput[]
}

export interface ChatUsage {
  model: string
  provider: string
  inputTokens: number
  outputTokens: number
  cost: Money
  cacheHit: boolean
}

export type ChatEvent =
  | { type: 'meta'; model: string; provider: string }
  | { type: 'token'; text: string }
  /** An action the AI proposes. Same shape as a recovery suggestion; it needs the user's approval. */
  | { type: 'suggestion'; suggestion: RecoverySuggestion }
  | ({ type: 'usage' } & ChatUsage)
  | { type: 'done' }
  | { type: 'error'; code: string }
