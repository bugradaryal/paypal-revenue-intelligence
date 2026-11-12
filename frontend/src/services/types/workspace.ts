import type { Money } from './common'
import type { RecoverySuggestion } from './revenue'

/** The client keeps the conversation; the backend completion endpoint receives its history per request. */

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
